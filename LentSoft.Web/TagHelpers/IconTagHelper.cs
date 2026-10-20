using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace LentSoft.Web.TagHelpers;

[HtmlTargetElement("icon", TagStructure = TagStructure.NormalOrSelfClosing)]
public class IconTagHelper : TagHelper
{
    private readonly IUrlHelperFactory _urlHelperFactory;

    public IconTagHelper(IUrlHelperFactory urlHelperFactory)
    {
        _urlHelperFactory = urlHelperFactory;
    }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    [HtmlAttributeName("name")]
    public string Name { get; set; } = string.Empty;

    [HtmlAttributeName("class")]
    public string? Class { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "svg";
        var baseClass = "icon";
        var finalClass = string.IsNullOrWhiteSpace(Class) ? baseClass : $"{baseClass} {Class.Trim()}";
        output.Attributes.SetAttribute("class", finalClass);

        if (!context.AllAttributes.ContainsName("aria-hidden") &&
            !context.AllAttributes.ContainsName("role") &&
            !context.AllAttributes.ContainsName("aria-label"))
        {
            output.Attributes.SetAttribute("aria-hidden", "true");
        }

        output.TagMode = TagMode.StartTagAndEndTag;

        var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);
        var spriteUrl = urlHelper.Content($"~/img/icons/sprite.svg#icon-{Name}");

        output.Content.SetHtmlContent($"<use href=\"{spriteUrl}\"></use>");
    }
}
