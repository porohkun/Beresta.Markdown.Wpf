namespace MarkdigWpfViewer.Tests;

/// <summary>Списки: маркеры, нумерация и пустые строки между пунктами.</summary>
public sealed class ListTests
{
    [Fact]
    public void Пустая_строка_между_пунктами_не_рвёт_список()
    {
        ModelDump.Of("- a\n\n- b").Should().Be("L[• P(a) _; • P(b)]");
    }

    [Fact]
    public void Нумерация_через_пустую_строку_сквозная()
    {
        ModelDump.Of("1. a\n\n1. b").Should().Be("L[1. P(a) _; 2. P(b)]");
    }

    [Fact]
    public void Нумерация_начинается_с_записанного_числа()
    {
        ModelDump.Of("3. a\n4. b").Should().Be("L[3. P(a); 4. P(b)]");
    }

    [Fact]
    public void Скобка_после_номера_сохраняется()
    {
        ModelDump.Of("1) a\n2) b").Should().Be("L[1) P(a); 2) P(b)]");
    }

    [Fact]
    public void Маркер_вложенного_списка_меняется_с_глубиной()
    {
        ModelDump.Of("- a\n  - b\n    - c\n      - d")
            .Should().Be("L[• P(a) L[◦ P(b) L[▪ P(c) L[• P(d)]]]]");
    }

    [Fact]
    public void Нумерованный_список_тоже_углубляет_вложенный()
    {
        ModelDump.Of("1. a\n   - b").Should().Be("L[1. P(a) L[◦ P(b)]]");
    }

    [Fact]
    public void Цитата_глубину_списка_не_меняет()
    {
        ModelDump.Of("> - a\n>   - b").Should().Be("Q[L[• P(a) L[◦ P(b)]]]");
    }

    [Fact]
    public void Отметка_задачи_показывается_исходным_текстом()
    {
        ModelDump.Of("- [ ] todo\n- [x] done").Should().Be("L[• P([ ] todo); • P([x] done)]");
    }
}