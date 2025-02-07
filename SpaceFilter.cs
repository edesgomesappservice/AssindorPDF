using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iTextSharp.text.pdf.parser;

namespace CloudDocs.AssinadorDigital
{
    public class SpaceFilter : RenderFilter
    {
        public override bool AllowText(TextRenderInfo renderInfo)
        {
            return renderInfo != null && renderInfo.GetText().Trim().Length > 0;
        }
    }
}
