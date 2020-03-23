﻿using Discord;
using Discord.Commands;
using NethysBot.Helpers;
using NethysBot.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

namespace NethysBot.Modules
{
	public class Combat : NethysBase<SocketCommandContext>
	{
		[Command("Encounter"),Alias("Enc", "Battle","Combat")]
		[RequireContext(ContextType.Guild)]
		public async Task NewBattle(EncArgs Args = EncArgs.Info)
		{
			Battle b = GetBattle(Context.Channel.Id);
			switch ((int)Args)
			{
				case 0:
					await ReplyAsync(" ", DisplayBattle(b,Context));
					return;
				case 1:
					if (Context.User.Id != b.Director)
					{
						await ReplyAsync(Context.Client.GetUser(b.Director).Username+ " is directing an encounter in this room already. To forcefully end this encounter, use the command `!ForceEnd` (Available only to users with the \"Manage Messages\" permission.)");
						return;
					}
					else if(Context.User.Id == b.Director && b.Active)
					{
						b.Participants = b.Participants.OrderBy(x => x.Initiative).ToList();
						b.CurrentTurn = b.Participants.First();
						UpdateBattle(b);
						await CurrentTurn(b,Context);
						return;
					}
					else
					{
						b.Participants = new List<Participant>();
						b.Director = Context.User.Id;
						b.Active = true;
						var embed = new EmbedBuilder().WithTitle("Roll for initiative!")
							.WithDescription(Context.User.Username + " has started a new encounter!")
							.AddField("Players", "Use the `!Join SkillName` command to enter initiative.\nYou can also use `!Join #` to add your initative number manually.",true)
							.AddField("Director", "Use `!AddNPC Name Initaitve` to add NPCs to the turn order.",true)
							.AddField("Ready to go?","Once all characters have been added, use the `!Encounter Start` command to start the encounter.");
						UpdateBattle(b);
						return;
					}
				case 2:
					if(Context.User.Id != b.Director)
					{
						await ReplyAsync("You aren't the director of this encounter! To forcefully end this battle, use the command `!ForceEnd` (Available only to users with the \"Manage Messages\" permission.)");
						return;
					}
					return;
			}
		}


		private Battle GetBattle(ulong channel)
		{
			var col = Database.GetCollection<Battle>("Battles");
			if (col.Exists(x => x.Channel == channel))
			{
				return col.FindOne(x => x.Channel == channel);
			}
			else
			{
				var b = new Battle()
				{
					Channel = channel,
				};
				col.Insert(b);
				col.EnsureIndex(x => x.Channel);
				return col.FindOne(x => x.Channel == channel);
			}
		}
		private void UpdateBattle(Battle b)
		{
			var col = Database.GetCollection<Battle>("Battles");
			col.Update(b);
		}
		public async Task CurrentTurn(Battle b, SocketCommandContext context)
		{
			var channel = context.Guild.GetTextChannel(b.Channel);

			if(b.CurrentTurn.Player > 0)
			{
				var player = context.Client.GetUser(b.CurrentTurn.Player);
				await channel.SendMessageAsync(player.Mention + ", " + b.CurrentTurn.Name + "'s turn!");
			}
			else
			{
				var player = context.Client.GetUser(b.Director);
				await channel.SendMessageAsync(player.Mention + ", " + b.CurrentTurn.Name + "'s turn!");
			}
		}
		public async Task NextTurn(Battle B, SocketCommandContext context)
		{
			int i = B.Participants.IndexOf(B.CurrentTurn);

			if (i + 1 >= B.Participants.Count) B.CurrentTurn = B.Participants.First();
			else B.CurrentTurn = B.Participants[i + 1];
			UpdateBattle(B);
			await CurrentTurn(B, context);
		}
		private Embed DisplayBattle(Battle b, SocketCommandContext context)
		{
			if (!b.Active)
			{
				return new EmbedBuilder().WithDescription("No encounter is currently being ran on this room.").Build();
			}

			var embed = new EmbedBuilder()
				.WithTitle("Encounter")
				.AddField("Game Master", context.Client.GetUser(b.Director).Mention, true);
			var summary = new StringBuilder();
			foreach(var p in b.Participants.OrderBy(x=>x.Initiative))
			{
				if (p.Name == b.CurrentTurn.Name) summary.AppendLine("`" + p.Initiative + "` - " + p.Name + " (Current)");
				else summary.AppendLine("`" + p.Initiative + "` - " + p.Name);
			}
			if (summary.ToString().NullorEmpty()) summary.AppendLine("No participants");

			embed.AddField("Participants", summary.ToString(), true);
			
			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);

			return embed.Build();
		}
		public enum EncArgs { Info = 0, Create = 1, New = 1, Start = 1, Stop = 2, End = 2, Delete = 2 };
	}
}