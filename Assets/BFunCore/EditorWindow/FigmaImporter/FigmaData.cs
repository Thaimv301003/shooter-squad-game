using System.Collections.Generic;
using Newtonsoft.Json;

namespace BFunCoreKit.Figma
{
    public class FigmaFileResponse
    {
        [JsonProperty("document")]
        public FigmaNode Document { get; set; }

        [JsonProperty("styles")]
        public Dictionary<string, FigmaStyleDef> Styles { get; set; }
    }

    public class FigmaStyleDef
    {
        [JsonProperty("key")] public string Key { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("styleType")] public string StyleType { get; set; }
    }

    public class FigmaNode
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } 

        [JsonProperty("children")]
        public List<FigmaNode> Children { get; set; }

        [JsonProperty("constraints")]
        public FigmaConstraints Constraints { get; set; }

        [JsonProperty("absoluteBoundingBox")]
        public FigmaBoundingBox AbsoluteBoundingBox { get; set; }

        [JsonProperty("absoluteRenderBounds")]
        public FigmaBoundingBox AbsoluteRenderBounds { get; set; }

        [JsonProperty("fills")]
        public List<FigmaPaint> Fills { get; set; }

        [JsonProperty("layoutMode")]
        public string LayoutMode { get; set; }

        [JsonProperty("itemSpacing")]
        public float ItemSpacing { get; set; }

        [JsonProperty("paddingLeft")]
        public float PaddingLeft { get; set; }
        
        [JsonProperty("paddingRight")]
        public float PaddingRight { get; set; }
        
        [JsonProperty("paddingTop")]
        public float PaddingTop { get; set; }
        
        [JsonProperty("paddingBottom")]
        public float PaddingBottom { get; set; }

        [JsonProperty("characters")]
        public string Characters { get; set; }
        
        [JsonProperty("style")]
        public FigmaTypeStyle Style { get; set; }

        [JsonProperty("styles")]
        public Dictionary<string, string> Styles { get; set; }
    }

    public class FigmaConstraints
    {
        [JsonProperty("horizontal")] public string Horizontal { get; set; }
        [JsonProperty("vertical")] public string Vertical { get; set; }
    }

    public class FigmaBoundingBox
    {
        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("y")] public float Y { get; set; }
        [JsonProperty("width")] public float Width { get; set; }
        [JsonProperty("height")] public float Height { get; set; }
    }

    public class FigmaPaint
    {
        [JsonProperty("type")] public string Type { get; set; } 
        [JsonProperty("color")] public FigmaColor Color { get; set; }
        [JsonProperty("visible")] public bool? Visible { get; set; }
    }

    public class FigmaColor
    {
        [JsonProperty("r")] public float R { get; set; }
        [JsonProperty("g")] public float G { get; set; }
        [JsonProperty("b")] public float B { get; set; }
        [JsonProperty("a")] public float A { get; set; }
    }

    public class FigmaTypeStyle
    {
        [JsonProperty("fontWeight")] public float FontWeight { get; set; }
        [JsonProperty("italic")] public bool Italic { get; set; }

        [JsonProperty("fontSize")] public float FontSize { get; set; }
        [JsonProperty("textAlignHorizontal")] public string TextAlignHorizontal { get; set; } 
        [JsonProperty("textAlignVertical")] public string TextAlignVertical { get; set; } 
        [JsonProperty("fills")] public List<FigmaPaint> Fills { get; set; }
    }

    public class FigmaImageResponse
    {
        [JsonProperty("err")] public string Error { get; set; }
        [JsonProperty("images")] public Dictionary<string, string> Images { get; set; }
    }
}
