using LinqToTwitter;
using System.Collections.Generic;
using System.Windows.Input;
using Twice.ViewModels.Main;
using Twice.ViewModels.Twitter;

namespace Twice.ViewModels.Profile
{
	internal interface IProfileDialogViewModel : IDialogViewModel, ILoadCallback
	{
		void Setup( ulong id );

		void Setup( string screenName );

		ICommand FollowUserCommand { get; }
		Friendship Friendship { get; }
		bool IsBusy { get; }
		ICommand UnfollowUserCommand { get; }
		UserViewModel User { get; }
		ICollection<UserSubPage> UserPages { get; }
	}
}