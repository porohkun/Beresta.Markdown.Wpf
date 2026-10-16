namespace MarkdigWpfViewer.Showcase.Showcases.Copying;

using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Selection;

/// <summary>Буфер обмена, прочитанный за одно открытие: все форматы, обычный текст и разметка.</summary>
internal sealed record ClipboardSnapshot(IReadOnlyList<string> Formats, string Text, string Markdown)
{
    private const uint UnicodeTextFormat = 13;

    /// <summary>Снимок буфера; <c>null</c> — буфер сейчас открыт другой программой.</summary>
    public static ClipboardSnapshot? TryRead()
    {
        if (!OpenClipboard(IntPtr.Zero))
        {
            return null;
        }

        try
        {
            var markdownFormat = (uint)DataFormats.GetDataFormat(MdDataFormats.Markdown).Id;
            var formats = new List<string>();
            var text = string.Empty;
            var markdown = string.Empty;

            for (var format = EnumClipboardFormats(0); format != 0; format = EnumClipboardFormats(format))
            {
                formats.Add(DataFormats.GetDataFormat((int)format).Name);
                if (format == UnicodeTextFormat)
                {
                    text = ReadText(format, Encoding.Unicode);
                }
                else if (format == markdownFormat)
                {
                    markdown = ReadText(format, Encoding.UTF8);
                }
            }

            return new(formats, text, markdown);
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static string ReadText(uint format, Encoding encoding)
    {
        var handle = GetClipboardData(format);
        var pointer = handle == IntPtr.Zero ? IntPtr.Zero : GlobalLock(handle);
        if (pointer == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            var bytes = new byte[(int)GlobalSize(handle)];
            Marshal.Copy(pointer, bytes, 0, bytes.Length);
            return encoding.GetString(bytes).TrimEnd('\0');
        }
        finally
        {
            GlobalUnlock(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr owner);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern uint EnumClipboardFormats(uint format);

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint format);

    [DllImport("kernel32.dll")]
    private static extern UIntPtr GlobalSize(IntPtr handle);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr handle);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr handle);
}
