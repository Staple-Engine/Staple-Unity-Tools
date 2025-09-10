using System.Collections.Generic;

namespace Staple
{
    public class MaterialMetadata
    {
        public string shader = "1ca9a72c-161e-44db-ad76-bf0ae432f78b";

        public Dictionary<string, MaterialParameter> parameters = new();

        public List<string> enabledShaderVariants = new();

        public CullingMode cullingMode = CullingMode.Back;
    }
}
