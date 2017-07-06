using System;
using System.ComponentModel;
using System.Numerics;
using System.Reflection;
using log4net;
using MiNET.Entities;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Worlds;
using MiNET;
using MiNET.Plugins;
using MiNET.Sounds;
using MiNET.Items;
using MiNET.Entities.Projectiles;
namespace xCore
{
	public class xCoreHealthManager : HealthManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(xCoreHealthManager));

		public xCoreGames Core { get; set; }

		public xCoreHealthManager(Entity entity, xCoreGames core) : base(entity)
		{
			Core = core;
			Entity = entity;
			ResetHealth();
		}

		public override void TakeHit(Entity source, Item item, int damage = 1, DamageCause cause = DamageCause.Unknown)
		{
			var player = Entity as Player;
			if (player != null && player.GameMode != GameMode.Survival) return;
			if (cause == DamageCause.Suffocation) return;
			if (CooldownTick > 0) return;

			if(source != null)
				LastDamageSource = source;
			if (!(LastDamageSource is Player))
				LastDamageSource = null;
			
			LastDamageCause = cause;
			if (Absorption > 0)
			{
				float abs = Absorption * 10;
				abs = abs - damage;
				if (abs < 0)
				{
					Absorption = 0;
					damage = Math.Abs((int)Math.Floor(abs));
				}
				else
				{
					Absorption = abs / 10f;
					damage = 0;
				}
			}

			if (cause == DamageCause.Starving)
			{
				if (Entity.Level.Difficulty <= Difficulty.Easy && Hearts <= 10) return;
				if (Entity.Level.Difficulty <= Difficulty.Normal && Hearts <= 1) return;
			}

			int plus = 0;
			/// <summary>
			/// The kill sync.
			/// </summary>
			Health -= (damage + plus) * 10;
			if (Health < 0)
			{
				//OnPlayerTakeHit(new HealthEventArgs(this, source, Entity));
				Health = 20;
				Kill();
				return;
			}

			if (player != null)
			{
				player.HungerManager.IncreaseExhaustion(0.3f);

				if (Health >= 1)
				{
					player.SendUpdateAttributes();
				}
			}

			if(Health > 0)
				Entity.BroadcastEntityEvent();

			if (source != null)
			{
				DoKnockback(source, item);
			}

			CooldownTick = 10;

			//OnPlayerTakeHit(new HealthEventArgs(this, source, Entity));
		}

		private object _killSync = new object();

		public override void Kill()
		{
			lock (_killSync)
			{
				if (IsDead) return;
				IsDead = true;

				Health = 200;
			}
			var player = Entity as Player;
			//Entity.BroadcastEntityEvent();

			if (player != null)
			{
				//player.SendUpdateAttributes();
				if (player.Level is xCoreLevel)
				{
					//player.StrikeLightning();
					((xCoreLevel)player.Level).Game.DeathPlayer(player);

					ResetHealth();
					player.HungerManager.ResetHunger();
					CooldownTick = 20;
					player.SendUpdateAttributes();
				}
				else {
					ResetHealth();
					player.HungerManager.ResetHunger();
					player.SendUpdateAttributes();
					CooldownTick = 20;
					player.Teleport(player.Level.SpawnPoint);
				}
			}
		}

		public override void OnTick()
		{
			if (CooldownTick > 0) CooldownTick--;

			if (!Entity.IsSpawned) return;

			if (IsDead) return;

			if (IsInvulnerable) Health = MaxHealth;

			if (Health <= 0)
			{
				Kill();
				return;
			}

			if (Entity.KnownPosition.Y < 0 && !IsDead)
			{
				TakeHit(null, 300, DamageCause.Void);
				return;
			}

			if (IsInWater(Entity.KnownPosition))
			{
				Entity.IsInWater = true;
				if (Entity is xPlayer) ((xPlayer)Entity).CooldownTick = 40;
				Air--;
				if (Air <= 0)
				{
					if (Math.Abs(Air) % 10 == 0)
					{
						TakeHit(null, 1, DamageCause.Drowning);
						//Entity.BroadcastSetEntityData();
					}
				}

				//Entity.BroadcastSetEntityData();

				if (IsOnFire)
				{
					IsOnFire = false;
					FireTick = 0;
					Entity.BroadcastSetEntityData();
				}
			}
			else
			{
				Air = MaxAir;

				if (Entity.IsInWater)
				{
					if (Entity is xPlayer) ((xPlayer)Entity).CooldownTick = 40;
					Entity.IsInWater = false;
					Entity.BroadcastSetEntityData();
				}

			}

			if (IsInSolid(Entity.KnownPosition))
			{
				if (SuffocationTicks <= 0)
				{
					TakeHit(null, 1, DamageCause.Suffocation);
					//Entity.BroadcastSetEntityData();

					SuffocationTicks = 10;
				}
				else
				{
					SuffocationTicks--;
				}
			}
			else
			{
				SuffocationTicks = 10;
			}

			if (IsInLavaOrFire(Entity.KnownPosition))
			{
				if (LastDamageCause.Equals(DamageCause.Lava))
				{
					FireTick += 2;
				}
				else
				{
					FireTick = 300;
					IsOnFire = true;
					Entity.BroadcastSetEntityData();
				}

				if (LavaTicks <= 0)
				{
					TakeHit(null, 4, DamageCause.Lava);
					Entity.BroadcastSetEntityData();

					LavaTicks = 10;
				}
				else
				{
					LavaTicks--;
				}
			}
			else
			{
				LavaTicks = 0;
			}

			if (!IsInLavaOrFire(Entity.KnownPosition) && IsOnFire)
			{
				FireTick--;
				if (FireTick <= 0)
				{
					IsOnFire = false;
					Entity.BroadcastSetEntityData();
				}

				if (Math.Abs(FireTick) % 20 == 0)
				{
					if (Entity is Player)
					{
						Player player = (Player)Entity;
						player.DamageCalculator.CalculatePlayerDamage(null, player, null, 1, DamageCause.FireTick);
						TakeHit(null, 1, DamageCause.FireTick);
					}
					else
					{
						TakeHit(null, 1, DamageCause.FireTick);
					}
					Entity.BroadcastSetEntityData();
				}
			}
		}


		private bool IsInWater(PlayerLocation playerPosition)
		{
			float y = playerPosition.Y + 1.62f;

			BlockCoordinates waterPos = new BlockCoordinates
			{
				X = (int)Math.Floor(playerPosition.X),
				Y = (int)Math.Floor(y),
				Z = (int)Math.Floor(playerPosition.Z)
			};

			var block = Entity.Level.GetBlock(waterPos);

			if (block == null || (block.Id != 8 && block.Id != 9)) return false;

			return y < Math.Floor(y) + 1 - ((1 / 9) - 0.1111111);
		}


		private bool IsInLavaOrFire(PlayerLocation playerPosition)
		{
			var block = Entity.Level.GetBlock(playerPosition);

			if (block == null || (block.Id != 10 && block.Id != 11 && block.Id != 51)) return false;

			return playerPosition.Y < Math.Floor(playerPosition.Y) + 1 - ((1 / 9) - 0.1111111);
		}

		private bool IsInSolid(PlayerLocation playerPosition)
		{
			BlockCoordinates solidPos = (BlockCoordinates)playerPosition;
			solidPos.Y += 1;

			var block = Entity.Level.GetBlock(solidPos);

			if (block == null) return false;

			return block.IsSolid;
		}
	}

	public class xCoreHungerManager : HungerManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(xCoreHungerManager));

		public xCoreHungerManager(Player player) : base(player)
		{
			Player = player;
			ResetHunger();
		}

		public override void ProcessHunger(bool forceSend = false)
		{
			if (HungerProcess) base.ProcessHunger(forceSend);
		}

		public void SetProcess(bool force = false)
		{
			HungerProcess = force;
			ResetHunger();
		}

		public Boolean HungerProcess = false;

		public Boolean Regen = true;

		public override void OnTick()
		{
			if (HungerProcess)
			{
				base.OnTick();
				return;
			}
			if (Regen)
			{
				Player.HealthManager.Health = Player.HealthManager.MaxHealth;
				Hunger = MaxHunger;
			}
			Hunger = MaxHunger;
		}
	}
}