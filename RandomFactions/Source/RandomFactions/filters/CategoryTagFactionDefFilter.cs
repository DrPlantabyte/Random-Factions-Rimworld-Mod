using RimWorld;
using Verse;

namespace RandomFactions.filters
{
    public class CategoryTagFactionDefFilter : FactionDefFilter
    {
        private string tag; // eg "Outlander"
        private bool exclude;
        public CategoryTagFactionDefFilter(string tag, bool exclude)
        {
            this.tag = tag;
            this.exclude = exclude;
        }

        public override bool Matches(FactionDef f)
        {
            bool is_category = this.tag.EqualsIgnoreCase(f.categoryTag);
            if (this.exclude)
            {
                return !is_category;
            } else
            {
                return is_category;
            }
        }
    }
}
