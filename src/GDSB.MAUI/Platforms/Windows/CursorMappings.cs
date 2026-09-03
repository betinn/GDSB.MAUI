using System.Reflection;
using GDSB.MAUI.Behaviors;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;

namespace GDSB.MAUI.Platforms.Windows
{
    // No WinUI o cursor de hover (UIElement.ProtectedCursor) é "protected" - sem API pública do MAUI
    // pra expor isso ainda, então setamos via reflection. É a técnica documentada pela comunidade
    // .NET MAUI/WinUI pra esse cenário, não um hack instável específico deste app.
    internal static class CursorMappings
    {
        private static readonly PropertyInfo? ProtectedCursorProperty =
            typeof(UIElement).GetProperty("ProtectedCursor", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Lazy<InputCursor> HandCursor =
            new(() => InputSystemCursor.Create(InputSystemCursorShape.Hand));

        public static void Apply()
        {
            // Todo Button do app é uma ação clicável (copiar, editar, favoritar, excluir, filtros...).
            ButtonHandler.Mapper.AppendToMapping("HoverCursor", (handler, _) => SetHand(handler.PlatformView));

            // Label/Grid usados como área clicável via TapGestureRecognizer (link de URL, linha da lista)
            // precisam de opt-in explícito via Behaviors.HoverCursor.IsHand="True" no XAML.
            LabelHandler.Mapper.AppendToMapping("HoverCursor", (handler, view) =>
            {
                if (view is BindableObject element && HoverCursor.GetIsHand(element))
                    SetHand(handler.PlatformView);
            });

            LayoutHandler.Mapper.AppendToMapping("HoverCursor", (handler, view) =>
            {
                if (view is BindableObject element && HoverCursor.GetIsHand(element))
                    SetHand(handler.PlatformView);
            });

            // Border não passa por LayoutHandler nem por LabelHandler - tem handler próprio. Sem
            // este mapeamento, uma pílula/cartão clicável (a entrada "Como funciona?" da tela de
            // desbloqueio) ficava com o cursor de seta, sem nenhuma pista de que dá para clicar.
            BorderHandler.Mapper.AppendToMapping("HoverCursor", (handler, view) =>
            {
                if (view is BindableObject element && HoverCursor.GetIsHand(element))
                    SetHand(handler.PlatformView);
            });
        }

        private static void SetHand(UIElement element) => ProtectedCursorProperty?.SetValue(element, HandCursor.Value);
    }
}
