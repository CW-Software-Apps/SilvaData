using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace SilvaData.Platforms.Android
{
    // Workaround para bug do AndroidX emoji2 com campos numéricos no Android 13+.
    //
    // Quando o usuário digita em um Entry numérico (Keyboard.Numeric / ValorInteiro),
    // o NumberKeyListener faz replace() no SpannableBuilder com índices que ficam fora
    // do bounds após a operação. O EmojiTextWatcher.afterTextChanged() então chama
    // EmojiCompat.process() com esses índices inválidos, lançando:
    //   java.lang.IllegalArgumentException: end should be < than charSequence length
    //
    // Desabilitar EmojiCompat no EditText impede a instalação do EmojiTextWatcher,
    // eliminando o crash. Sem efeito colateral visível — emojis em campos de texto
    // normal continuam funcionando pois MAUI recria o handler para cada Entry.
    public class NumericEntryHandler : EntryHandler
    {
        protected override MauiAppCompatEditText CreatePlatformView()
        {
            var view = base.CreatePlatformView();
            view.SetEmojiCompatEnabled(false);
            return view;
        }
    }
}
