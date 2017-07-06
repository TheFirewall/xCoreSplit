using MiNET;
using MiNET.Utils;
using MiNET.Worlds;
using MiNET.Net;
using MiNET.Entities;

namespace xCore
{

	public class NoDamageHealthManager : HealthManager
	{
		public NoDamageHealthManager(Entity entity) : base(entity)
		{
		}

		public override void TakeHit(Entity source, int damage = 1, DamageCause cause = DamageCause.Unknown)
		{
			//base.TakeHit(source, 0, cause);
		}

		public override void OnTick()
		{
		}
	}

	public class Boss : Entity
	{
		public bool IsEnabled { get; set; }

		public bool UpdateTag { get; set; } = true;

		public string OldNameTag { get; set; } = "";

		public MetadataDictionary Meta { get; set; }

		public Boss(Level level, int MaxValue = 20, int StartValue = 1) : base((int)EntityType.Slime, level)
		{
			//Gravity = 0;
			level.EntityManager.AddEntity(this);
			NameTag = "§l§eДобро пожаловать на §3Cristalix §aPocket Edition";
			KnownPosition = level.SpawnPoint;
			//HealthManager.MaxHealth = MaxValue;
			//HealthManager.Health = StartValue;
			/*long flags = 0;
			flags |= 1 << 5;
			flags |= 1 << 16;
			flags |= 1 << 17;
			HideNameTag = true;
			IsInvisible = true;
			NoAi = true;
			MetadataDictionary metadata = new MetadataDictionary();
			metadata[(int)MetadataFlags.EntityFlags] = new MetadataLong(flags);
			metadata[(int)MetadataFlags.HideNameTag] = new MetadataByte(!HideNameTag);
			metadata[(int)MetadataFlags.NameTag] = new MetadataString(NameTag ?? string.Empty);
			//metadata[(int)MetadataFlags.Scale] = new Metadata
			Meta = metadata;
			HealthManager.MaxHealth = 1;
			HealthManager.Health = 1;
			HealthManager.IsInvulnerable = true;*/

			Width = 0;
			Length = 0;
			Height = 0;

			HideNameTag = true;
			IsAlwaysShowName = false;
			HealthManager = new NoDamageHealthManager(this);
		}

		//public override MetadataDictionary GetMetadata()
		//{
		//	return Meta;
		//}

		public void SetNameTag(xCoreLevel level, string s, bool staticq = false)
		{
			level.OldNameTag = level.NameTag;
			level.NameTag = s;
			if (staticq)
			{
				level.SendNameTag = false;
				level.UpdateTag = true;
				return;
			}
			if (level.OldNameTag != s)
			{
				level.NameTag = s;
				level.SendNameTag = true;
				level.UpdateTag = true;
			}
			else {
				level.SendNameTag = true;
				level.UpdateTag = false;
			}
		}

		public void SetInt(xCoreLevel level, int max, int i)
		{
			level.MaxInt = max;
			level.Int = i;
		}

		public void SendBossEventBar(Player player, int type)
		{
			var BossEvent = McpeBossEvent.CreateObject();
			//BossEvent.WriteSignedVarLong(EntityId);
			//BossEvent.WriteUnsignedVarLong(type);
			BossEvent.bossEntityId = EntityId;
			BossEvent.eventType = (uint)type;
			player.SendPackage(BossEvent);
		}

		public void SendBossEventBar(Level level, int type)
		{
			var BossEvent = McpeBossEvent.CreateObject();
			//BossEvent.WriteSignedVarLong(EntityId);
			//BossEvent.WriteUnsignedVarLong(type);
			BossEvent.bossEntityId = EntityId;
			BossEvent.eventType = (uint)type;
			level.RelayBroadcast(BossEvent);
		}

		public void SendName(Player player, string Name)
		{
			MetadataDictionary metadata = GetMetadata();
			metadata[(int)MetadataFlags.NameTag] = Name;
			McpeSetEntityData mcpeSetEntityData = McpeSetEntityData.CreateObject();
			mcpeSetEntityData.runtimeEntityId = EntityId;
			mcpeSetEntityData.metadata = metadata;
			player.SendPackage(mcpeSetEntityData);
		}

		public void SendName(Level level, string Name)
		{
			MetadataDictionary metadata = GetMetadata();
			metadata[(int)MetadataFlags.NameTag] = Name;
			McpeSetEntityData mcpeSetEntityData = McpeSetEntityData.CreateObject();
			mcpeSetEntityData.runtimeEntityId = EntityId;
			mcpeSetEntityData.metadata = metadata;
			//player.SendPackage(mcpeSetEntityData);
			level.RelayBroadcast(mcpeSetEntityData);
		}

		public virtual void SendAttributes(Player player)
		{
			var attributes = new PlayerAttributes
			{
				["minecraft:health"] = new PlayerAttribute
				{
					Name = "minecraft:health",
					MinValue = 0,
					MaxValue = 20,
					Value = 20,
					Default = 20
				}
			};

			// Workaround, bad design.

			McpeUpdateAttributes attributesPackate = McpeUpdateAttributes.CreateObject();
			attributesPackate.runtimeEntityId = EntityId;
			attributesPackate.attributes = attributes;
			//Level.RelayBroadcast(attributesPackate);
			player.SendPackage(attributesPackate);
		}

		public virtual void SendAttributes(Level level)
		{
			PlayerAttributes attributes;
			if (level is xCoreLevel)
			{
				attributes = new PlayerAttributes();
				attributes["minecraft:health"] = new PlayerAttribute
				{
					Name = "minecraft:health",
					MinValue = 0,
					MaxValue = ((xCoreLevel)level).MaxInt,
					Value = ((xCoreLevel)level).Int,
					Default = ((xCoreLevel)level).MaxInt
				};
			}
			else {
				attributes = new PlayerAttributes();
				attributes["minecraft:health"] = new PlayerAttribute
				{
					Name = "minecraft:health",
					MinValue = 0,
					MaxValue = 20,
					Value = 20,
					Default = 20
				};
			}

			// Workaround, bad design.

			McpeUpdateAttributes attributesPackate = McpeUpdateAttributes.CreateObject();
			attributesPackate.runtimeEntityId = EntityId;
			attributesPackate.attributes = attributes;
			level.RelayBroadcast(attributesPackate);
			//player.SendPackage(attributesPackate);
		}

		public virtual void Kill(Player player)
		{
			var msg = McpeEntityEvent.CreateObject();
			msg.runtimeEntityId = EntityId;
			msg.eventId = (byte)3;
			player.SendPackage(msg);
		}

		public virtual void Kill(Level level)
		{
			var msg = McpeEntityEvent.CreateObject();
			msg.runtimeEntityId = EntityId;
			msg.eventId = (byte)3;
			level.RelayBroadcast(msg);
		}

		public override void OnTick()
		{
			base.OnTick();
		}

		public void SpawnToPlayers(Player player)
		{
			var addEntity = McpeAddEntity.CreateObject();
			addEntity.entityType = (byte)EntityTypeId;
			addEntity.entityIdSelf = EntityId;
			addEntity.runtimeEntityId = EntityId;
			addEntity.x = KnownPosition.X;
			addEntity.y = KnownPosition.Y;
			addEntity.z = KnownPosition.Z;
			addEntity.yaw = KnownPosition.Yaw;
			addEntity.pitch = KnownPosition.Pitch;
			addEntity.metadata = GetMetadata();
			addEntity.speedX = (float)Velocity.X;
			addEntity.speedY = (float)Velocity.Y;
			addEntity.speedZ = (float)Velocity.Z;
			addEntity.attributes = GetEntityAttributes();
			player.SendPackage(addEntity);

			SendAttributes(player);

			var BossEvent = McpeBossEvent.CreateObject();
			//BossEvent.WriteSignedVarLong(EntityId);
			BossEvent.bossEntityId = EntityId;
			BossEvent.eventType = 0;
			//BossEvent.WriteSignedVarLong(0);
			player.SendPackage(BossEvent);
		}
	}
}