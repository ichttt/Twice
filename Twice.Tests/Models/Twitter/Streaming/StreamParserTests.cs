﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LinqToTwitter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Twice.Models.Twitter;
using Twice.Models.Twitter.Streaming;

namespace Twice.Tests.Models.Twitter.Streaming
{
	[TestClass, ExcludeFromCodeCoverage]
	public class StreamParserTests
	{
		[TestMethod, TestCategory( "Models.Twitter.Streaming" )]
		public async Task ConnectionIsClosedOnDispose()
		{
			// Arrange
			var stream = new Mock<IStreaming>();
			stream.Setup( s => s.CloseStream() ).Verifiable();

			var streamingList = new List<IStreaming>
			{
				stream.Object
			};

			var userStream = new Mock<IStreamingConnection>();
			userStream.Setup( s => s.Start( It.IsAny<Func<IStreamContent, Task>>() ) ).Returns( Task.FromResult( streamingList ) );

			var parser = StreamParser.Create( userStream.Object, null );

			parser.StartStreaming();

			// Act
			await parser.StreamingTask;
			parser.Dispose();

			// Assert
			stream.Verify( s => s.CloseStream(), Times.Once() );
		}

		[TestMethod, TestCategory( "Models.Twitter.Streaming" )]
		public async Task KeepAliveMessageIsIgnored()
		{
			// Arrange
			const string strContent = "";
			var parser = SetupParser( strContent );

			// Act
			parser.StartStreaming();
			await parser.StreamingTask;

			// Assert
		}

		[TestMethod, TestCategory( "Models.Twitter.Streaming" )]
		public void ReceivingFavoriteRaisesEvent()
		{
			// Arrange
			const string strContent =
				"{ \"event\":\"favorite\", \"created_at\": \"Sat Sep 04 16:10:54 +0000 2010\", \"target\": { \"id\": 123 }, \"source\": { \"id\": 456 }, \"target_object\": { \"created_at\": \"Wed Jun 06 20:07:10 +0000 2012\", \"id_str\": \"210462857140252672\" } }";

			ManualResetEventSlim waitHandle = new ManualResetEventSlim( false );
			var parser = SetupParser( strContent );
			EventStreamEventArgs receivedData = null;

			parser.FavoriteEventReceived += ( s, e ) =>
			{
				receivedData = e;
				waitHandle.Set();
			};

			// Act
			parser.StartStreaming();
			waitHandle.Wait( 1000 );

			// Assert
			Assert.IsNotNull( receivedData );
			Assert.AreEqual( StreamEventType.Favorite, receivedData.Event );
			Assert.AreEqual( 456ul, receivedData.Source.GetUserId() );
			Assert.AreEqual( 123ul, receivedData.Target.GetUserId() );
		}

		[TestMethod, TestCategory( "Models.Twitter.Streaming" )]
		public void ReceivingFriendlistRaisesEvent()
		{
			// Arrange
			const string strContent = "{\"friends\":[123,456,789]}";

			ManualResetEventSlim waitHandle = new ManualResetEventSlim( false );
			var parser = SetupParser( strContent );
			FriendsStreamEventArgs receivedData = null;

			parser.FriendsReceived += ( s, e ) =>
			{
				receivedData = e;
				waitHandle.Set();
			};

			// Act
			parser.StartStreaming();
			waitHandle.Wait( 1000 );

			// Assert
			Assert.IsNotNull( receivedData );
			CollectionAssert.AreEqual( new ulong[] {123, 456, 789}, receivedData.Friends );
		}

		[TestMethod, TestCategory( "Models.Twitter.Streaming" )]
		public void ReceivingMessageDeleteRaisesEvent()
		{
			// Arrange
			const string strContent =
				"{\"delete\":{\"direct_message\":{\"id\":1234,\"id_str\":\"1234\",\"user_id\":3,\"user_id_str\":\"3\"}}}";

			ManualResetEventSlim waitHandle = new ManualResetEventSlim( false );
			var parser = SetupParser( strContent );
			DeleteStreamEventArgs receivedDelete = null;

			parser.DirectMessageDeleted += ( s, e ) =>
			{
				receivedDelete = e;
				waitHandle.Set();
			};

			// Act
			parser.StartStreaming();
			waitHandle.Wait( 1000 );

			// Assert
			Assert.IsNotNull( receivedDelete );
			Assert.AreEqual( 1234ul, receivedDelete.Id );
			Assert.AreEqual( 3ul, receivedDelete.UserId );
		}

		[TestMethod, TestCategory( "Models.Twitter.Streaming" )]
		public void ReceivingMessageRaisesEvent()
		{
			// Arrange
			var strContent = File.ReadAllText( "Data/message.json" );

			ManualResetEventSlim waitHandle = new ManualResetEventSlim( false );
			var parser = SetupParser( strContent );
			DirectMessageStreamEventArgs receivedData = null;

			parser.DirectMessageReceived += ( s, e ) =>
			{
				receivedData = e;
				waitHandle.Set();
			};

			// Act
			parser.StartStreaming();
			waitHandle.Wait( 1000 );

			// Assert
			Assert.IsNotNull( receivedData );
			Assert.AreNotEqual( 0ul, receivedData.Message.GetMessageId() );
		}

		[TestMethod, TestCategory( "Models.Twitter.Streaming" )]
		public void ReceivingStatusDeleteRaisesEvent()
		{
			// Arrange
			const string strContent = "{\"delete\":{\"status\":{\"id\":1234,\"id_str\":\"1234\",\"user_id\":3,\"user_id_str\":\"3\"}}}";

			ManualResetEventSlim waitHandle = new ManualResetEventSlim( false );
			var parser = SetupParser( strContent );
			DeleteStreamEventArgs receivedDelete = null;

			parser.StatusDeleted += ( s, e ) =>
			{
				receivedDelete = e;
				waitHandle.Set();
			};

			// Act
			parser.StartStreaming();
			waitHandle.Wait( 1000 );

			// Assert
			Assert.IsNotNull( receivedDelete );
			Assert.AreEqual( 1234ul, receivedDelete.Id );
			Assert.AreEqual( 3ul, receivedDelete.UserId );
		}

		[TestMethod, TestCategory( "Models.Twitter.Streaming" )]
		public void ReceivingStatusRaisesEvent()
		{
			// Arrange
			var strContent = File.ReadAllText( "Data/tweet.json" );

			ManualResetEventSlim waitHandle = new ManualResetEventSlim( false );
			var parser = SetupParser( strContent );
			StatusStreamEventArgs receivedData = null;

			parser.StatusReceived += ( s, e ) =>
			{
				receivedData = e;
				waitHandle.Set();
			};

			// Act
			parser.StartStreaming();
			waitHandle.Wait( 1000 );

			// Assert
			Assert.IsNotNull( receivedData );
			Assert.AreNotEqual( 0ul, receivedData.Status.StatusID );
		}

		[TestMethod, TestCategory( "Models.Twitter.Streaming" )]
		public void ReceivingUnknownDataRaisesEvent()
		{
			// Arrange
			const string strContent = "{\"test\":[1,2,3]}";

			ManualResetEventSlim waitHandle = new ManualResetEventSlim( false );
			var parser = SetupParser( strContent );
			StreamEventArgs receivedData = null;

			parser.UnknownDataReceived += ( s, e ) =>
			{
				receivedData = e;
				waitHandle.Set();
			};

			// Act
			parser.StartStreaming();
			waitHandle.Wait( 1000 );

			// Assert
			Assert.IsNotNull( receivedData );
			Assert.AreEqual( strContent, receivedData.Json );
		}

		[TestMethod, TestCategory( "Models.Twitter.Streaming" )]
		public void ReceivingUnkownDataRaisesEvent()
		{
			// Arrange
			const string strContent =
				"{ \"event\":\"unknown\", \"created_at\": \"Sat Sep 04 16:10:54 +0000 2010\", \"target\": { \"id\": 123 }, \"source\": { \"id\": 456 }, \"target_object\": { \"created_at\": \"Wed Jun 06 20:07:10 +0000 2012\", \"id_str\": \"210462857140252672\" } }";

			ManualResetEventSlim waitHandle = new ManualResetEventSlim( false );
			var parser = SetupParser( strContent );
			EventStreamEventArgs receivedData = null;

			parser.UnknownEventReceived += ( s, e ) =>
			{
				receivedData = e;
				waitHandle.Set();
			};

			// Act
			parser.StartStreaming();
			waitHandle.Wait( 1000 );

			// Assert
			Assert.IsNotNull( receivedData );
		}

		[TestMethod, TestCategory( "Models.Twitter.Streaming" )]
		public void StreamIsNotStartedTwice()
		{
			// Arrange
			var userStream = new Mock<IStreamingConnection>();
			userStream.Setup( s => s.Start( It.IsAny<Func<IStreamContent, Task>>() ) )
				.Returns( Task.FromResult( new List<IStreaming>() ) ).Verifiable();

			var parser = StreamParser.Create( userStream.Object, null );

			// Act
			parser.StartStreaming();
			parser.StartStreaming();

			// Assert
			userStream.Verify( s => s.Start( It.IsAny<Func<IStreamContent, Task>>() ), Times.AtMostOnce() );
		}

		[TestMethod, TestCategory( "Models.Twitter.Streaming" )]
		public void StreamParserCanBeCreated()
		{
			// Arrange
			var userStream = new Mock<IStreamingConnection>();

			// Act
			var parser = StreamParser.Create( userStream.Object, null );

			// Assert
			Assert.IsNotNull( parser );
		}

		private static StreamParser SetupParser( string strContent )
		{
			var execute = new Mock<ITwitterExecute>();
			StreamContent content = new StreamContent( execute.Object, strContent );

			var stream = new Mock<IStreamingConnection>();
			stream.Setup( s => s.Start( It.IsAny<Func<IStreamContent, Task>>() ) )
				.Callback<Func<StreamContent, Task>>( func => func( content ) )
				.Returns( Task.FromResult( new List<IStreaming>() ) );

			var parser = StreamParser.Create( stream.Object, null );
			return parser;
		}
	}
}