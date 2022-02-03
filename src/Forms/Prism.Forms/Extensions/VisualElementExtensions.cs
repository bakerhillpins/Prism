using System;
using Xamarin.Forms;

namespace Prism.Extensions
{
    internal static class VisualElementExtensions
    {
        public static Element GetRoot(this Element element)
        {
            return element.Parent switch
                   {
                       null => element,
                       _ => GetRoot(element.Parent)
                   };
        }

        public static bool TryGetParentPage( this VisualElement visualElement, out Page page )
        {
            page = GetParentPage( visualElement );
            return page != null;
        }

        private static Page GetParentPage(Element element)
        {
            return element as Page ?? element.Parent switch
                                      {
                                          null => null,
                                          _ => GetParentPage( element.Parent )
                                      };
        }
    }
}
