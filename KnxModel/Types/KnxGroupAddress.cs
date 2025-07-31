namespace KnxModel
{
    public record KnxGroupAddress (string MainGroup, string MiddleGroup, string SubGroup)
    {
        public string Address => $"{MainGroup}/{MiddleGroup}/{SubGroup}";
    }
}
