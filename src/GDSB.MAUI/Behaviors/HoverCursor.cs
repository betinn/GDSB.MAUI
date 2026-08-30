namespace GDSB.MAUI.Behaviors
{
    // Propriedade anexada cross-platform (não faz nada fora do Windows): marca um elemento que não é
    // Button (Label/Grid usados como área clicável via TapGestureRecognizer) para ganhar o cursor de
    // mão no hover. Buttons já ganham isso globalmente via Platforms/Windows/CursorMappings.cs.
    public static class HoverCursor
    {
        public static readonly BindableProperty IsHandProperty =
            BindableProperty.CreateAttached("IsHand", typeof(bool), typeof(HoverCursor), false);

        public static bool GetIsHand(BindableObject view) => (bool)view.GetValue(IsHandProperty);

        public static void SetIsHand(BindableObject view, bool value) => view.SetValue(IsHandProperty, value);
    }
}
