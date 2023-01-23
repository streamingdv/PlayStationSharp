﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Flurl.Http;
using Newtonsoft.Json;
using PlayStationSharp.Exceptions;
using PlayStationSharp.Exceptions.Message;
using PlayStationSharp.Model;

namespace PlayStationSharp.API
{
	public enum MessageType
	{
		MessageText,
		MessageAudio,
		MessageImage
	}

	public class MessageThread : IPlayStation
	{
		public PlayStationClient Client { get; private set; }

		public ThreadModel Information { get; private set; }

		private readonly Lazy<List<Message>> _messages;
		private readonly Lazy<List<User>> _members;

		public List<Message> Messages => _messages.Value;
		public List<User> Members => _members.Value;

		private MessageThread(PlayStationClient client)
		{
			Client = client;
			_messages = new Lazy<List<Message>>(() => GetMessages());
			_members = new Lazy<List<User>>(GetMembers);
		}

		public MessageThread(PlayStationClient client, ThreadModel thread) : this(client)
		{
			Information = thread;
		}

		public MessageThread(PlayStationClient client, string threadId) : this(client)
		{
			Information = GetThread(threadId);
		}

		/// <summary>
		/// Gets a message thread from a thread id.
		/// </summary>
		/// <param name="threadId">Id of the thread.</param>
		/// <param name="count">Amount of messages to return.</param>
		/// <returns>New instance of ThreadModel.</returns>
		private ThreadModel GetThread(string threadId, int count = 200)
		{
			try
			{
				return Request.SendGetRequest<ThreadModel>(
					$"https://us-gmsg.np.community.playstation.net/groupMessaging/v1/threads/{threadId}?count={count}&fields=threadMembers,threadNameDetail,threadThumbnailDetail,threadProperty,latestTakedownEventDetail,newArrivalEventDetail,threadEvents",
					this.Client.Tokens.Authorization);
			}
			catch (PlayStationApiException ex)
			{
				switch (ex.Error.Code)
				{
					case 2121741:
						throw new ThreadNotFoundException();
					default:
						throw;
				}
			}
		}

		/// <summary>
		/// Gets all members in the thread.
		/// </summary>
		/// <returns>List of each user.</returns>
		private List<User> GetMembers()
		{
			return this.Information.ThreadMembers.Select(member => new User(Client, member.OnlineId)).ToList();
		}

		/// <summary>
		/// Leave the current message thread.
		/// </summary>
		/// <returns>If left successfully.</returns>
		private bool Leave()
		{
			try
			{
				Request.SendDeleteRequest<object>(
					$"https://us-gmsg.np.community.playstation.net/groupMessaging/v1/threads/{this.Information.ThreadId}/users/me",
					Client.Tokens.Authorization);

				return true;
			}
			catch (PlayStationApiException ex)
			{
				switch (ex.Error.Code)
				{
					case 2121741:
						throw new ThreadNotFoundException();
					default:
						throw;
				}
			}
		}

		/// <summary>
		/// Gets all messages in the message thread.
		/// </summary>
		/// <param name="count">Amount of messages to return.</param>
		/// <returns>List of new Message objects.</returns>
		private List<Message> GetMessages(int count = 200)
		{
			var messages = new List<Message>();

			var threadModels = GetThread(this.Information.ThreadId, count);

			return threadModels.ThreadEvents.Select(message => new Message(Client, message)).ToList();
		}

		/// <summary>
		/// Send a message to the current thread.
		/// </summary>
		/// <param name="content">Body of the message.</param>
		/// <returns>New instance of the message thread.</returns>
		public MessageThread SendMessage(string content)
		{
			var data = new
			{
				messageEventDetail = new
				{
					eventCategoryCode = 1,
					messageDetail = new
					{
						body = content
					}
				}
			};

			var requestBody = new StringBuilder();

			requestBody.Append("--gc0p4Jq0M2Yt08jU534c0p");
			requestBody.AppendLine();
			requestBody.Append("Content-Type: application/json; charset=utf-8");
			requestBody.AppendLine();
			requestBody.Append("Content-Disposition: form-data; name=\"messageEventDetail\"");
			requestBody.AppendLine();
			requestBody.AppendLine();
			requestBody.Append(JsonConvert.SerializeObject(data, Formatting.Indented));
			requestBody.AppendLine();
			requestBody.Append("--gc0p4Jq0M2Yt08jU534c0p--");

			var messageModel = Request.SendMultiPartPostRequest<CreatedThreadModel>(
				$"https://us-gmsg.np.community.playstation.net/groupMessaging/v1/threads/{this.Information.ThreadId}/messages", requestBody, "gc0p4Jq0M2Yt08jU534c0p", this.Client.Tokens.Authorization);

			return this;
		}

	}
}