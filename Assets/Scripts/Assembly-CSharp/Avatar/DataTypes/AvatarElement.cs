namespace Avatar.DataTypes
{
    public class AvatarElement : BaseEntitlement
    {
        public int    ReferenceId   { get; private set; }
        public int    DefaultTint   { get; private set; }
        public string Category      { get; private set; }
        public bool   IsRandomizable { get; private set; }

        public AvatarElement(string aId, int aReferenceId, string aName, string aCategory, string aSd, string aHd, string aThumb, bool aIsRandomizable, int aDefaultTint)
        {
            Id             = aId;
            ReferenceId    = aReferenceId;
            Name           = aName;
            Category       = aCategory;
            Sd             = aSd;
            Hd             = aHd;
            Thumb          = aThumb;
            IsRandomizable = aIsRandomizable;
            DefaultTint    = aDefaultTint;
        }
    }
}
