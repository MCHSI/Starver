﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;

namespace Starvers
{
	public class StarverPlayer
	{
		#region Guest
		private class GuestPlayer : StarverPlayer
		{
			private static PlayerData NoneData = new PlayerData(-4);
			public override PlayerData Data => NoneData;
			public override double DamageIndex => 0;
			public GuestPlayer(int idx)
			{
				Index = idx;
			}
		}
		#endregion
		public int Index { get; protected set; }
		public virtual PlayerData Data { get; }
		public virtual Player TPlayer => Main.player[Index];
		public virtual TSPlayer TSPlayer => TShock.Players[Index];
		public int Timer { get; protected set; }
		#region From TPlayer
		public int ItemUseDelay { get; set; }
		public Item HeldItem => TPlayer.inventory[TPlayer.selectedItem];
		public double ItemUseAngle
		{
			get
			{
				double angle = TPlayer.itemRotation;
				if (TPlayer.direction == -1)
				{
					angle += Math.PI;
				}
				return angle;
			}
		}
		public bool ControlUseItem => TPlayer.controlUseItem;
		#region Center
		public Vector2 Center
		{
			get
			{
				return TPlayer.Center;
			}
			set
			{
				TPlayer.Center = value;
				SendData(PacketTypes.PlayerUpdate, "", Index);
			}
		}
		#endregion
		#region Position
		public Vector2 Position
		{
			get
			{
				return TPlayer.position;
			}
			set
			{
				TPlayer.position = value;
				SendData(PacketTypes.PlayerUpdate, "", Index);
			}
		}
		#endregion
		#region Velocity
		public Vector2 Velocity
		{
			get
			{
				return TPlayer.velocity;
			}
			set
			{
				TPlayer.velocity = value;
				SendData(PacketTypes.PlayerUpdate, "", Index);
			}
		}
		#endregion
		#endregion



		public int Exp
		{
			get => Data.Exp;
			set => OnExpChange(Exp, value);
		}

		public int Level
		{
			get => Data.Level;
			set => OnLevelChange(Level, value);
		}
#warning 还没做
		public bool IsVip { get; set; }

		public virtual double DamageIndex
		{
			get
			{
				int level = Level;
				return level switch
				{
					_ when level < 100 => 1 + 0.015 * level,
					_ when 100 <= level && level < 1000 => 1 + 1.5 + Math.Log(level / 100, 2),
					// _ when 1000 <= level && level < 10000 => 1 + 1.5 + Math.Pow(level, 0.2) + 3 * Math.Pow(level / 10000, 10) - 4
					_ when 1000 <= level && level < 10000 => 1.821928094887362 + Math.Pow(level, 0.2) + 3 * Math.Pow(level / 10000, 10),
					_ when 10000 <= level && level < 100000 => 11.131501539689296 + Math.Pow(Math.Log10(level) - 3.7, Math.Log(level / 1000, 2) + 1),
					_ => 20
				};
			}
		}

		protected StarverPlayer()
		{
			
		}

		public StarverPlayer(int index)
		{
			Index = index;
			try
			{
				Data = Starver.Instance.PlayerDatas.GetData(TSPlayer.Account.ID);
			}
			catch(KeyNotFoundException)
			{
				Data = new PlayerData(TSPlayer.Account.ID);
				Starver.Instance.PlayerDatas.SaveData(Data);
			}
		}

		public virtual void SaveData()
		{
			Starver.Instance.PlayerDatas.SaveData(Data);
		}
		public int CalcUpgradeLevel()
		{
			int divide = IsVip ? 3 : 1;
			return CalcUpgradeLevel(Level) / divide;
		}
		#region Projs
		#region FromPolar
		/// <summary>
		/// 极坐标获取角度
		/// </summary>
		/// <param name="angle">所需角度(弧度)</param>
		/// <param name="radius"></param>
		/// <returns></returns>
		public Vector FromPolar(double angle, float radius)
		{
			return Vector.FromPolar(angle, radius);
		}
		#endregion
		#region NewProj
		/// <summary>
		/// 生成弹幕
		/// </summary>
		/// <param name="position"></param>
		/// <param name="velocity"></param>
		/// <param name="Type"></param>
		/// <param name="Damage"></param>
		/// <param name="KnockBack"></param>
		/// <param name="ai0"></param>
		/// <param name="ai1"></param>
		/// <returns></returns>
		public int NewProj(Vector2 position, Vector2 velocity, int Type, int Damage, float KnockBack = 20f, float ai0 = 0, float ai1 = 0)
		{
			return Utils.NewProj(position, velocity, Type, Damage, KnockBack, Index, ai0, ai1);
		}
		/// <summary>
		/// 生成弹幕
		/// </summary>
		public int NewProj(Vector2 velocity, int Type, int Damage, float KnockBack = 20f, float ai0 = 0, float ai1 = 0)
		{
			return Utils.NewProj(Center, velocity, Type, Damage, KnockBack, Index, ai0, ai1);
		}
		#endregion
		#region ProjCircle
		/// <summary>
		/// 弹幕圆
		/// </summary>
		/// <param name="Center"></param>
		/// <param name="r"></param>
		/// <param name="speed">向外速率</param>
		/// <param name="Type"></param>
		/// <param name="number">弹幕总数</param>
		public void ProjCircle(Vector2 Center, float r, float speed, int Type, int number, int Damage, float ai0 = 0, float ai1 = 0)
		{
			double averagerad = Math.PI * 2 / number;
			for (int i = 0; i < number; i++)
			{
				NewProj(Center + FromPolar(averagerad * i, r), FromPolar(averagerad * i, speed), Type, Damage, 4f, ai0, ai1);
			}
		}
		/// <summary>
		/// 弹幕圆
		/// </summary>
		/// <param name="Center"></param>
		/// <param name="angle">偏转角</param>
		/// <param name="r"></param>
		/// <param name="speed">速率</param>
		/// <param name="Type"></param>
		/// <param name="number">弹幕总数</param>
		public void ProjCircleEx(Vector2 Center, double angle, float r, float speed, int Type, int number, int Damage, float ai0 = 0, float ai1 = 0)
		{
			double averagerad = Math.PI * 2 / number;
			for (int i = 0; i < number; i++)
			{
				NewProj(Center + FromPolar(angle + averagerad * i, r), FromPolar(angle + averagerad * i, speed), Type, Damage, 4f, ai0, ai1);
			}
		}

		/// <summary>
		/// 弹幕圆
		/// </summary>
		/// <param name="Center"></param>
		/// <param name="angle">偏转角</param>
		/// <param name="r"></param>
		/// <param name="velocity">速度</param>
		/// <param name="Type"></param>
		/// <param name="number">弹幕总数</param>
		public void ProjCircleEx(Vector2 Center, double angle, float r, Vector2 velocity, int Type, int number, int Damage, float ai0 = 0, float ai1 = 0)
		{
			double averagerad = Math.PI * 2 / number;
			for (int i = 0; i < number; i++)
			{
				NewProj(Center + FromPolar(angle + averagerad * i, r), velocity, Type, Damage, 4f, ai0, ai1);
			}
		}

		/// <summary>
		/// 弹幕圆
		/// </summary>
		/// <param name="Center"></param>
		/// <param name="r"></param>
		/// <param name="speed">向外速率</param>
		/// <param name="Type"></param>
		/// <param name="number">弹幕总数</param>
		public int[] ProjCircleRet(Vector2 Center, float r, float speed, int Type, int number, int Damage, float ai0 = 0, float ai1 = 0)
		{
			double averagerad = Math.PI * 2 / number;
			int[] arr = new int[number];
			for (int i = 0; i < number; i++)
			{
				arr[i] = NewProj(Center + FromPolar(averagerad * i, r), FromPolar(averagerad * i, speed), Type, Damage, 4f, ai0, ai1);
			}
			return arr;
		}
		#endregion
		#region ProjSector
		/// <summary>
		/// 扇形弹幕
		/// </summary>
		/// <param name="Center">圆心</param>
		/// <param name="speed">向外速率</param>
		/// <param name="r">半径</param>
		/// <param name="interrad">中心半径的方向</param>
		/// <param name="rad">张角</param>
		/// <param name="Damage">伤害(带加成)</param>
		/// <param name="type"></param>
		/// <param name="num">数量</param>
		/// <param name="ai0"></param>
		/// <param name="ai1"></param>
		public void ProjSector(Vector2 Center, float speed, float r, double interrad, double rad, int Damage, int type, int num, float ai0 = 0, float ai1 = 0)
		{
			double start = interrad - rad / 2;
			double average = rad / num;
			for (int i = 0; i < num; i++)
			{
				NewProj(Center + FromPolar(start + i * average, r), FromPolar(start + i * average, speed), type, Damage, 4f, ai0, ai1);
			}
		}
		#endregion
		#region ProjLine
		/// <summary>
		/// 制造速度平行的弹幕直线
		/// </summary>
		/// <param name="Begin">起点</param>
		/// <param name="End">终点</param>
		/// <param name="Vel">速度</param>
		/// <param name="num">数量</param>
		/// <param name="Damage"></param>
		/// <param name="type"></param>
		/// <param name="ai0"></param>
		/// <param name="ai1"></param>
		public void ProjLine(Vector2 Begin, Vector2 End, Vector2 Vel, int num, int Damage, int type, float ai0 = 0, float ai1 = 0)
		{
			Vector2 average = End - Begin;
			average /= num;
			for (int i = 0; i < num; i++)
			{
				NewProj(Begin + average * i, Vel, type, Damage, 3f, ai0, ai1);
			}
		}
		/// <summary>
		/// 制造速度平行的弹幕直线
		/// </summary>
		/// <param name="Begin">起点</param>
		/// <param name="End">终点</param>
		/// <param name="Vel">速度</param>
		/// <param name="num">数量</param>
		/// <param name="Damage"></param>
		/// <param name="type"></param>
		/// <param name="ai0"></param>
		/// <param name="ai1"></param>
		public int[] ProjLineReturns(Vector2 Begin, Vector2 End, Vector2 Vel, int num, int Damage, int type, float ai0 = 0, float ai1 = 0)
		{
			int[] arr = new int[num];
			Vector2 average = End - Begin;
			average /= num;
			for (int i = 0; i < num; i++)
			{
				arr[i] = NewProj(Begin + average * i, Vel, type, Damage, 3f, ai0, ai1);
			}
			return arr;
		}
		#endregion
		#endregion
		#region OnXXChange
		private void OnExpChange(int oldValue, int newValue)
		{
			if (Level > Starver.Instance.Config.AutoUpgradeLevel)
			{
				int divide = IsVip ? 3 : 1;
				var (exp, lvl) = (newValue, Level);
				int expNeed = CalcUpgradeLevel(lvl) / divide;
				while (exp >= expNeed)
				{
					exp -= expNeed;
					lvl++;
					expNeed = CalcUpgradeLevel(lvl) / divide;
				}
				(Exp, Level) = (exp, lvl);
			}
		}
		private void OnLevelChange(int oldValue,int newValue)
		{
			Data.Level = newValue;
		}
		#endregion
		#region Events
		private void OnUseItem(Item item)
		{

		}
		public virtual void OnGetData(GetDataEventArgs args)
		{
			switch(args.MsgID)
			{
				case PacketTypes.PlayerAnimation:
					{
						using var stream = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length);
						using var reader = new BinaryReader(stream);
						int index = reader.ReadByte();
						index = Index;
						Player player3 = TPlayer;
						var itemRotation = reader.ReadSingle();
						int itemAnimation = reader.ReadInt16();
						TPlayer.itemRotation = itemRotation;
						TPlayer.itemAnimation = itemAnimation;
						TPlayer.channel = HeldItem.channel;
						if (ItemUseDelay == 0 && ControlUseItem)
						{
							OnUseItem(HeldItem);
							ItemUseDelay += HeldItem.useTime;
						}
						args.Handled = true;
						break;
					}
			}
		}
		public virtual void OnLeave()
		{

		}
		public virtual void OnStrikeNpc(NpcStrikeEventArgs args)
		{
			args.Damage = (int)(args.Damage * DamageIndex);
			args.Npc.SendCombatText(args.Damage.ToString(), Starver.DamageColor);
			var realdamage = (int)Main.CalculateDamageNPCsTake(args.Damage, args.Npc.defense);
			Exp += realdamage;
		}
		public virtual void Update()
		{
			Timer++;
			if (Timer % 60 == 0)
			{
				SendStatusText($"Level: {Level}\nExp:{Exp}");
			}
			if (ItemUseDelay > 0)
			{
				ItemUseDelay--;
			}
		}
		#endregion
		#region Utilities
		#region Buff
		public void AddBuffIfNot(int type, int time = 60 * 60)
		{
			if (!HasBuff(type))
			{
				SetBuff(type, time);
			}
		}
		public void SetBuff(int type, int time = 3600, bool bypass = false)
		{
			TSPlayer.SetBuff(type, time, bypass);
		}
		public bool HasBuff(int type)
		{
			return FindBuffIndex(type) != -1;
		}
		public void RemoveBuff(int type)
		{
			int idx = FindBuffIndex(type);
			if (idx != -1)
			{
				TPlayer.buffTime[idx] = 0;
				TPlayer.buffType[idx] = 0;
				SendData(PacketTypes.PlayerBuff, "", Index);
			}
		}
		public int FindBuffIndex(int type)
		{
			int idx = -1;
			for (int i = 0; i < TPlayer.buffType.Length; i++)
			{
				if (TPlayer.buffType[i] == type)
				{
					idx = i;
					break;
				}
			}
			return idx;
		}
		#endregion
		#region Sends
		public void SendData(PacketTypes msgType, string text = "", int number = 0, float number2 = 0, float number3 = 0, float number4 = 0, int number5 = 0)
		{
			NetMessage.SendData((int)msgType, Index, -1, NetworkText.FromLiteral(text), number, number2, number3, number4, number5);
		}
		public void SendText(string text, Color color)
		{
			TSPlayer.SendMessage(text, color);
		}
		public void SendText(string text, byte r, byte g, byte b)
		{
			TSPlayer.SendMessage(text, r, g, b);
		}
		public void SendBlueText(string text)
		{
			SendText(text, 0, 0, 255);
		}
		public void SendErrorText(string text)
		{
			SendText(text, 255, 0, 0);
		}
		private static readonly string EndLine19 = new string('\n', 19);
		private static readonly string EndLine20 = new string('\n', 20);
		public void SendStatusText(string text)
		{
			text = EndLine19 + text + EndLine20;
			SendData(PacketTypes.Status, text);
		}
		#endregion
		#endregion
		#region Statics
		public static StarverPlayer GetGuest(int idx) => new GuestPlayer(idx);
		public static int CalcUpgradeLevel(int lvl)
		{
			if (lvl < 1000)
			{
				return (int)(lvl * 10.5f);
			}
			else if (lvl < 2500)
			{
				return (int)(lvl * 15.5f);
			}
			else if (lvl < (int)1e4)
			{
				return Math.Min(150000, (int)(lvl * Math.Log(lvl)));
			}
			else if (lvl < (int)1e5)
			{
				return 150000;
			}
			else
			{
				return 25 * 25 * 25 * 25;
			}
		}
		#endregion
	}
}
