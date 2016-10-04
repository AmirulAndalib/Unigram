// <auto-generated/>
using System;

namespace Telegram.Api.TL
{
	public partial class TLInputBotInlineMessageGame : TLInputBotInlineMessageBase 
	{
		[Flags]
		public enum Flag : Int32
		{
			ReplyMarkup = (1 << 2),
		}

		public bool HasReplyMarkup { get { return Flags.HasFlag(Flag.ReplyMarkup); } set { Flags = value ? (Flags | Flag.ReplyMarkup) : (Flags & ~Flag.ReplyMarkup); } }

		public Flag Flags { get; set; }

		public TLInputBotInlineMessageGame() { }
		public TLInputBotInlineMessageGame(TLBinaryReader from, bool cache = false)
		{
			Read(from, cache);
		}

		public override TLType TypeId { get { return TLType.InputBotInlineMessageGame; } }

		public override void Read(TLBinaryReader from, bool cache = false)
		{
			Flags = (Flag)from.ReadInt32();
			if (HasReplyMarkup) ReplyMarkup = TLFactory.Read<TLReplyMarkupBase>(from, cache);
			if (cache) ReadFromCache(from);
		}

		public override void Write(TLBinaryWriter to, bool cache = false)
		{
			UpdateFlags();

			to.Write(0x4B425864);
			to.Write((Int32)Flags);
			if (HasReplyMarkup) to.WriteObject(ReplyMarkup, cache);
			if (cache) WriteToCache(to);
		}

		private void UpdateFlags()
		{
			HasReplyMarkup = ReplyMarkup != null;
		}
	}
}