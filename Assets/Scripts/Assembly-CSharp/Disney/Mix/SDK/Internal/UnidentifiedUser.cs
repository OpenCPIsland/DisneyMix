using System;
using UnityEngine;

namespace Disney.Mix.SDK.Internal
{
	public class UnidentifiedUser : IInternalUnidentifiedUser, IUnidentifiedUser
	{
		private readonly IUserDatabase userDatabase;

		public IDisplayName DisplayName { get; private set; }

		public string FirstName { get; private set; }

		public IInternalAvatar InternalAvatar
		{
			get
			{
				if (DisplayName == null || string.IsNullOrEmpty(DisplayName.Text))
				{
					Debug.LogWarning("[UnidentifiedUser] Missing DisplayName while resolving avatar.");
					return AvatarBuilder.Build((AvatarDocument)null);
				}

				UserDocument userByDisplayName = userDatabase.GetUserByDisplayName(DisplayName.Text);
				if (userByDisplayName == null)
				{
					Debug.LogWarning("[UnidentifiedUser] UserDocument not found for displayName: " + DisplayName.Text);
					return AvatarBuilder.Build((AvatarDocument)null);
				}

				AvatarDocument avatar = userDatabase.GetAvatar(userByDisplayName.AvatarId);
				return AvatarBuilder.Build(avatar);
			}
		}

		public IAvatar Avatar
		{
			get
			{
				return InternalAvatar;
			}
		}

		public event EventHandler<AbstractAvatarChangedEventArgs> OnAvatarChanged = delegate
		{
		};

		public UnidentifiedUser(IDisplayName displayName, string firstName, IUserDatabase userDatabase)
		{
			this.userDatabase = userDatabase;
			DisplayName = displayName;
			FirstName = firstName;
		}

		public void DispatchOnAvatarChanged()
		{
			this.OnAvatarChanged(this, new AvatarChangedEventArgs(Avatar));
		}
	}
}
