using RimWorld;
using Verse;

namespace RandomFactions.filters
{
    public class CategoryTagFactionFilter : FactionFilter
    {
        private string tag; // eg "Outlander"
        private bool exclude;
        public CategoryTagFactionFilter(string tag, bool exclude)
        {
            this.tag = tag;
            this.exclude = exclude;
        }

        public override bool Matches(Faction f)
        {
            bool is_category = this.tag.EqualsIgnoreCase(f.def.categoryTag);
            if (this.exclude)
            {
                return !is_category;
            }
            else
            {
                return is_category;
            }
        }
    }
}
