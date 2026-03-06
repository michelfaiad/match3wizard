namespace Match3Wizard
{
    public enum ElementType
    {
        Fire  = 0,
        Water = 1,
        Air   = 2,
        Earth = 3,
        Light = 4
    }

    public static class ElementTypeExtensions
    {
        public static bool GeneratesMana(this ElementType e) => e != ElementType.Light;

        public static string DisplayName(this ElementType e) => e switch
        {
            ElementType.Fire  => "Fogo",
            ElementType.Water => "Água",
            ElementType.Air   => "Ar",
            ElementType.Earth => "Terra",
            ElementType.Light => "Luz",
            _                 => e.ToString()
        };
    }
}
