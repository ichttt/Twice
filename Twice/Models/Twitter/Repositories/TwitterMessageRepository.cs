using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Fody;
using LinqToTwitter;
using Twice.Models.Cache;

namespace Twice.Models.Twitter.Repositories
{
	[ExcludeFromCodeCoverage]
	[ConfigureAwait( false )]
	internal class TwitterMessageRepository : TwitterRepositoryBase, ITwitterMessageRepository
	{
		public TwitterMessageRepository( TwitterContext context, ICache cache ) : base( context, cache )
		{
		}

		public async Task<List<DirectMessage>> IncomingMessages( int count = 50, ulong? maxId = null )
		{
			// ReSharper disable RedundantBoolCompare
			var query = Queryable.Where( dm => dm.Type == DirectMessageType.SentTo && dm.Count == count && dm.FullText == true && dm.IncludeEntities == true );
			// ReSharper restore RedundantBoolCompare
			if( maxId.HasValue && maxId.Value != ulong.MaxValue )
			{
				query = query.Where( dm => dm.MaxID == maxId.Value );
			}

			var list = await query.ToListAsync();
			var result = new List<DirectMessage>( list.Count );
			foreach( var dm in list )
			{
				if( dm?.Recipient != null )
				{
					result.Add( dm );
				}
			}
			return result;
		}

		public async Task<List<DirectMessage>> OutgoingMessages( int count = 50, ulong? maxId = null )
		{
			// ReSharper disable RedundantBoolCompare
			var query = Queryable.Where( dm => dm.Type == DirectMessageType.SentBy && dm.Count == count && dm.FullText == true && dm.IncludeEntities == true );
			// ReSharper restore RedundantBoolCompare
			if( maxId.HasValue && maxId.Value != ulong.MaxValue )
			{
				query = query.Where( dm => dm.MaxID == maxId.Value );
			}

			var list = await query.ToListAsync();
			var result = new List<DirectMessage>( list.Count );
			foreach( var dm in list )
			{
				if( dm?.Recipient != null )
				{
					result.Add( dm );
				}
			}
			return result;
		}

		public Task<DirectMessage> SendMessage( string recipient, string message )
		{
			return Context.NewDirectMessageAsync( recipient, message );
		}

		private TwitterQueryable<DirectMessage> Queryable => Context.DirectMessage;
	}
}