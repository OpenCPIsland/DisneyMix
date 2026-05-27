namespace Avatar.DataTypes
{
	public class AvatarElement : BaseEntitlement
	{
		public int ReferenceId;

		public int DefaultTint;

		public string Category { get; private set; }

		public bool IsRandomizable { get; private set; }

		public AvatarElement(string aId, int aReferenceId, string aName, string aCategory, string aSd, string aHd, string aThumb, bool aIsRandomizable, int aDefaultTint)
		{
			base.Id = aId;
			ReferenceId = aReferenceId;
			base.Name = aName;
			Category = aCategory;
			base.Sd = aSd;
			base.Hd = aHd;
			base.Thumb = aThumb;
			IsRandomizable = aIsRandomizable;
			DefaultTint = aDefaultTint;
		}
	}
}
