using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iTextSharp.text.pdf.parser;

namespace CloudDocs.AssinadorDigital
{
    public class TextRenderInfoSplitter : IRenderListener
    {
        public TextRenderInfoSplitter(IRenderListener strategy)
        {
            this.strategy = strategy;
        }

        public void RenderText(TextRenderInfo renderInfo)
        {
            foreach (TextRenderInfo info in renderInfo.GetCharacterRenderInfos())
            {
                strategy.RenderText(info);
            }
        }

        public void BeginTextBlock()
        {
            strategy.BeginTextBlock();
        }

        public void EndTextBlock()
        {
            strategy.EndTextBlock();
        }

        public void RenderImage(ImageRenderInfo renderInfo)
        {
            strategy.RenderImage(renderInfo);
        }

        IRenderListener strategy;
    }
}
