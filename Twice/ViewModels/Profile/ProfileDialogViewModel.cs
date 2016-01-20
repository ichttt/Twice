﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LinqToTwitter;
using Twice.Models.Twitter;
using Twice.Resources;
using Twice.ViewModels.Twitter;

namespace Twice.ViewModels.Profile
{
	internal class ProfileDialogViewModel : DialogViewModel, IProfileDialogViewModel
	{
		public void Setup( ulong profileId )
		{
			ProfileId = profileId;
		}

		public async Task OnLoad( object data )
		{
			if( ProfileId == 0 )
			{
				Debugger.Break();
				return;
			}

			IsBusy = true;
			Context = ContextList.Contexts.First();

			User = await Context.Twitter.User.Where( tw => tw.Type == UserType.Show && tw.UserID == ProfileId ).SingleOrDefaultAsync();
			if( User == null )
			{
				// TODO: Handle errors
				return;
			}

			Friendship = await Context.Twitter.Friendship.Where( f => f.Type == FriendshipType.Show && f.SourceUserID == Context.UserId && f.TargetUserID == User.UserID ).SingleOrDefaultAsync();

			UserPages = new List<UserSubPage>
			{
				new UserSubPage( Strings.Tweets, LoadStatuses ),
				new UserSubPage( Strings.Following, LoadFollowings ),
				new UserSubPage( Strings.Followers, LoadFollowers )
			};
			RaisePropertyChanged( nameof( UserPages ) );

			IsBusy = false;
		}

		private async Task<IEnumerable<object>> LoadFollowers()
		{
			// ReSharper disable once RedundantBoolCompare (No results are found when omitting ==true)
			var users = await Context.Twitter.Friendship.Where( f => f.Type == FriendshipType.FollowersList && f.UserID == User.UserID.ToString() && f.Count == 200 && f.SkipStatus == true ).SelectMany( f => f.Users ).ToListAsync();

			return users.Select( u => new UserViewModel( u ) );
		}

		private async Task<IEnumerable<object>> LoadFollowings()
		{
			// ReSharper disable once RedundantBoolCompare (No results are found when omitting ==true)
			var users = await Context.Twitter.Friendship.Where( f => f.Type == FriendshipType.FriendsList && f.UserID == User.UserID.ToString() && f.Count == 200 && f.SkipStatus == true ).SelectMany( f => f.Users ).ToListAsync();

			return users.Select( u => new UserViewModel( u ) );
		}

		private async Task<IEnumerable<object>> LoadStatuses()
		{
			//IEnumerable<Status> cached = Services.GetService<IStatusCache>().GetStatusesOfUser( ProfileID );
			IEnumerable<Status> cached = Enumerable.Empty<Status>().ToArray();
			IEnumerable<Status> newStatuses;

			if( cached.Any() )
			{
				ulong since = cached.Max( c => c.StatusID );

				newStatuses = await Context.Twitter.Status.Where( s => s.Type == StatusType.User && s.UserID == User.UserID && s.SinceID == since ).ToListAsync();
			}
			else
			{
				newStatuses = await Context.Twitter.Status.Where( s => s.Type == StatusType.User && s.UserID == User.UserID ).ToListAsync();
			}

			return cached.Concat( newStatuses ).OrderByDescending( s => s.StatusID ).Select( s => new StatusViewModel( s, Context ) );
		}

		public Friendship Friendship
		{
			[DebuggerStepThrough]
			get
			{
				return _Friendship;
			}
			set
			{
				if( _Friendship == value )
				{
					return;
				}

				_Friendship = value;
				RaisePropertyChanged();
			}
		}

		public bool IsBusy
		{
			[DebuggerStepThrough]
			get
			{
				return _IsBusy;
			}
			set
			{
				if( _IsBusy == value )
				{
					return;
				}

				_IsBusy = value;
				RaisePropertyChanged();
			}
		}

		public User User
		{
			[DebuggerStepThrough]
			get
			{
				return _User;
			}
			set
			{
				if( _User == value )
				{
					return;
				}

				_User = value;
				RaisePropertyChanged();
			}
		}

		public ICollection<UserSubPage> UserPages { get; private set; }

		[DebuggerBrowsable( DebuggerBrowsableState.Never )]
		private Friendship _Friendship;

		[DebuggerBrowsable( DebuggerBrowsableState.Never )]
		private bool _IsBusy;

		[DebuggerBrowsable( DebuggerBrowsableState.Never )]
		private User _User;

		private IContextEntry Context;
		private ulong ProfileId;
	}
}