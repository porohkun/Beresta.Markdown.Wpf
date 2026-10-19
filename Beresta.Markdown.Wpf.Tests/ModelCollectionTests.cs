namespace Beresta.Markdown.Wpf.Tests;

using System.Collections.Specialized;
using Identity;
using ViewModels;

/// <summary>Список блоков, который меняется правками.</summary>
public sealed class ModelCollectionTests
{
    [Fact]
    public void Список_без_общих_элементов_заменяется_одним_уведомлением()
    {
        var collection = new ModelCollection<BlockVm>();
        collection.Apply(Blocks(1, 2, 3));
        var actions = Record(collection);

        collection.Apply(Blocks(4, 5, 6, 7));

        actions.Should().Equal(NotifyCollectionChangedAction.Reset);
        collection.Select(block => block.NodeId).Should().Equal(4, 5, 6, 7);
    }

    [Fact]
    public void Правка_оставляет_общие_элементы_на_месте()
    {
        var collection = new ModelCollection<BlockVm>();
        collection.Apply(Blocks(1, 2, 3, 4));
        var kept = collection[0];
        var actions = Record(collection);

        collection.Apply([collection[0], collection[1], collection[2], .. Blocks(5)]);

        actions.Should().NotContain(NotifyCollectionChangedAction.Reset);
        collection[0].Should().BeSameAs(kept);
        collection.Select(block => block.NodeId).Should().Equal(1, 2, 3, 5);
    }

    private static List<BlockVm> Blocks(params long[] ids)
        => ids
            .Select(id => (BlockVm)new ParagraphVm([]) { NodeId = id })
            .ToList();

    private static List<NotifyCollectionChangedAction> Record(ModelCollection<BlockVm> collection)
    {
        var actions = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);
        return actions;
    }
}
