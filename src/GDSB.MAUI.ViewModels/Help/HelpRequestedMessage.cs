namespace GDSB.MAUI.Help
{
    // Publicada pelo botão "?" (FieldLabelView / HelpButton) e recebida pelo HelpSheetView da
    // página que está aparecendo, via WeakReferenceMessenger do CommunityToolkit.Mvvm. Passar pelo
    // messenger evita duas coisas ruins: percorrer a árvore visual atrás do painel e acoplar o
    // ContentView do "?" à página que por acaso o hospeda.
    public sealed record HelpRequestedMessage(string TopicId);
}
