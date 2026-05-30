using System.Linq;
using System.Numerics;
using System.Text;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed class FoodSequenceSystem : SharedFoodSequenceSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodSequenceStartPointComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FoodSequenceStartPointComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<FoodSequenceStartPointComponent, CanDropTargetEvent>(OnCanDropTarget);
        SubscribeLocalEvent<FoodSequenceStartPointComponent, IngestedEvent>(OnStartIngested);
        SubscribeLocalEvent<FoodSequenceStartPointComponent, FullyEatenEvent>(OnStartFullyEaten);
        SubscribeLocalEvent<BurgerEntitiesComponent, IngestedEvent>(OnBurgerIngested);
        SubscribeLocalEvent<BurgerEntitiesComponent, FullyEatenEvent>(OnBurgerFullyEaten);

        SubscribeLocalEvent<FoodMetamorphableByAddingComponent, FoodSequenceIngredientAddedEvent>(OnIngredientAdded);
    }

    private void OnInteractUsing(Entity<FoodSequenceStartPointComponent> ent, ref InteractUsingEvent args)
    {
        if (TryComp<FoodSequenceElementComponent>(args.Used, out var sequenceElement) &&
            TryAddFoodElement(ent, (args.Used, sequenceElement), args.User))
        {
            args.Handled = true;
            return;
        }

        args.Handled = TryAddArbitraryEntity(ent, args.Used, args.User);
    }

    private void OnDragDropTarget(Entity<FoodSequenceStartPointComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        if (TryAddArbitraryEntity(ent, args.Dragged, args.User))
        {
            args.Handled = true;
        }
    }

    private void OnCanDropTarget(Entity<FoodSequenceStartPointComponent> ent, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop = true;
    }

    private void OnStartIngested(Entity<FoodSequenceStartPointComponent> ent, ref IngestedEvent args)
    {
        ConsumeContainedEntities(ent.Owner, args.User);
    }

    private void OnStartFullyEaten(Entity<FoodSequenceStartPointComponent> ent, ref FullyEatenEvent args)
    {
        ConsumeContainedEntities(ent.Owner, args.User);
    }

    private void OnBurgerIngested(Entity<BurgerEntitiesComponent> ent, ref IngestedEvent args)
    {
        ConsumeContainedEntities(ent.Owner, args.User);
    }

    private void OnBurgerFullyEaten(Entity<BurgerEntitiesComponent> ent, ref FullyEatenEvent args)
    {
        ConsumeContainedEntities(ent.Owner, args.User);
    }

    private void OnIngredientAdded(Entity<FoodMetamorphableByAddingComponent> ent, ref FoodSequenceIngredientAddedEvent args)
    {
        if (!TryComp<FoodSequenceStartPointComponent>(args.Start, out var start))
            return;

        if (!_proto.Resolve(args.Proto, out var elementProto))
            return;

        if (!ent.Comp.OnlyFinal || elementProto.Final || start.FoodLayers.Count == start.MaxLayers)
        {
            TryMetamorph((ent, start));
        }
    }

    private bool TryMetamorph(Entity<FoodSequenceStartPointComponent> start)
    {
        List<MetamorphRecipePrototype> availableRecipes = new();
        foreach (var recipe in _proto.EnumeratePrototypes<MetamorphRecipePrototype>())
        {
            if (recipe.Key != start.Comp.Key)
                continue;

            bool allowed = true;
            foreach (var rule in recipe.Rules)
            {
                if (!rule.Check(_proto, EntityManager, start, start.Comp.FoodLayers))
                {
                    allowed = false;
                    break;
                }
            }
            if (allowed)
                availableRecipes.Add(recipe);
        }

        if (availableRecipes.Count <= 0)
            return true;

        Metamorf(start, _random.Pick(availableRecipes)); //In general, if there's more than one recipe, the yml-guys screwed up. Maybe some kind of unit test is needed.
        PredictedQueueDel(start.Owner);
        return true;
    }

    private void Metamorf(Entity<FoodSequenceStartPointComponent> start, MetamorphRecipePrototype recipe)
    {
        var result = PredictedSpawnNextToOrDrop(recipe.Result, start);

        //Try putting in container
        _transform.DropNextTo(result, (start, Transform(start)));

        if (!_solutionContainer.TryGetSolution(result, start.Comp.Solution, out var resultSoln, out var resultSolution))
            return;

        if (!_solutionContainer.TryGetSolution(start.Owner, start.Comp.Solution, out var startSoln, out var startSolution))
            return;

        _solutionContainer.RemoveAllSolution(resultSoln.Value); //Remove all YML reagents
        resultSoln.Value.Comp.Solution.MaxVolume = startSoln.Value.Comp.Solution.MaxVolume;
        _solutionContainer.TryAddSolution(resultSoln.Value, startSolution);

        MergeFlavorProfiles(start, result);
        MergeTrash(start.Owner, result);
        MergeTags(start, result);

        if (_container.TryGetContainer(start.Owner, "burger_entities", out var oldContainer))
        {
            var newContainer = _container.EnsureContainer<Container>(result, "burger_entities");
            var entities = new List<EntityUid>(oldContainer.ContainedEntities);
            foreach (var ent in entities)
            {
                _container.Insert(ent, newContainer);
            }
            EnsureComp<BurgerEntitiesComponent>(result);
        }

        UpdateFoodNameFromResult(result);
    }

    private bool TryAddFoodElement(Entity<FoodSequenceStartPointComponent> start, Entity<FoodSequenceElementComponent, EdibleComponent?> element, EntityUid? user = null)
    {
        // we can't add a live mouse to a burger.
        if (!Resolve(element, ref element.Comp2, false))
            return false;

        if (element.Comp2.RequireDead && _mobState.IsAlive(element))
            return false;

        //looking for a suitable FoodSequence prototype
        if (!element.Comp1.Entries.TryGetValue(start.Comp.Key, out var elementProto))
            return false;

        if (!_proto.Resolve(elementProto, out var elementIndexed))
            return false;

        //if we run out of space, we can still put in one last, final finishing element.
        if (start.Comp.FoodLayers.Count >= start.Comp.MaxLayers && !elementIndexed.Final || start.Comp.Finished)
        {
            if (user is not null)
                _popup.PopupClient(Loc.GetString("food-sequence-no-space"), start, user.Value);
            return false;
        }

        // Prevents plushies with items hidden in them from being added to prevent deletion of items
        // If more of these types of checks need to be added, this should be changed to an event or something.
        if (TryComp<SecretStashComponent>(element, out var stashComponent) && stashComponent.ItemContainer.Count != 0)
        {
            return false;
        }

        //Generate new visual layer
        var flip = start.Comp.AllowHorizontalFlip && _random.Prob(0.5f);
        var layer = new FoodSequenceVisualLayer(elementIndexed,
            _random.Pick(elementIndexed.Sprites),
            new Vector2(flip ? -elementIndexed.Scale.X : elementIndexed.Scale.X, elementIndexed.Scale.Y),
            new Vector2(
                _random.NextFloat(start.Comp.MinLayerOffset.X, start.Comp.MaxLayerOffset.X),
                _random.NextFloat(start.Comp.MinLayerOffset.Y, start.Comp.MaxLayerOffset.Y))
        );

        start.Comp.FoodLayers.Add(layer);
        Dirty(start);

        if (elementIndexed.Final)
            start.Comp.Finished = true;

        UpdateFoodName(start);
        MergeFoodSolutions(start.Owner, element.Owner);
        MergeFlavorProfiles(start, element);
        MergeTrash(start.Owner, element.Owner);
        MergeTags(start, element);

        var ev = new FoodSequenceIngredientAddedEvent(start, element, elementProto, user);
        RaiseLocalEvent(start, ev);

        PredictedQueueDel(element.Owner);
        return true;
    }

    private void UpdateFoodName(Entity<FoodSequenceStartPointComponent> start)
    {
        if (start.Comp.NameGeneration is null)
            return;

        var content = new StringBuilder();
        var separator = "";
        if (start.Comp.ContentSeparator is not null)
            separator = Loc.GetString(start.Comp.ContentSeparator);

        HashSet<ProtoId<FoodSequenceElementPrototype>> existedContentNames = new();
        foreach (var layer in start.Comp.FoodLayers)
        {
            if (!existedContentNames.Contains(layer.Proto))
                existedContentNames.Add(layer.Proto);
        }

        var nameCounter = 1;
        foreach (var proto in existedContentNames)
        {
            if (!_proto.Resolve(proto, out var protoIndexed))
                continue;

            if (protoIndexed.Name is null)
                continue;

            content.Append(Loc.GetString(protoIndexed.Name.Value));

            if (nameCounter < existedContentNames.Count)
                content.Append(separator);
            nameCounter++;
        }

        var baseName = Loc.GetString(start.Comp.NameGeneration.Value,
            ("prefix", start.Comp.NamePrefix is not null ? Loc.GetString(start.Comp.NamePrefix) : ""),
            ("content", content),
            ("suffix", start.Comp.NameSuffix is not null ? Loc.GetString(start.Comp.NameSuffix) : ""));

        List<string> customIngredientNames = new();
        if (_container.TryGetContainer(start.Owner, "burger_entities", out var container))
        {
            foreach (var ent in container.ContainedEntities)
            {
                customIngredientNames.Add(MetaData(ent).EntityName);
            }
        }

        if (customIngredientNames.Count > 0)
        {
            var customContent = "";
            if (customIngredientNames.Count == 1)
            {
                customContent = customIngredientNames[0];
            }
            else if (customIngredientNames.Count == 2)
            {
                customContent = $"{customIngredientNames[0]} and {customIngredientNames[1]}";
            }
            else
            {
                customContent = string.Join(", ", customIngredientNames.SkipLast(1)) + $", and {customIngredientNames.Last()}";
            }
            baseName = $"{baseName} with {customContent}";
        }

        _metaData.SetEntityName(start, baseName);
    }

    private void MergeFoodSolutions(Entity<EdibleComponent?> start, Entity<EdibleComponent?> element)
    {
        if (!Resolve(start, ref start.Comp, false))
            return;

        if (!Resolve(element, ref element.Comp, false))
            return;

        if (!_solutionContainer.TryGetSolution(start.Owner, start.Comp.Solution, out var startSolutionEntity, out var startSolution))
            return;

        if (!_solutionContainer.TryGetSolution(element.Owner, element.Comp.Solution, out _, out var elementSolution))
            return;

        startSolution.MaxVolume += elementSolution.MaxVolume;
        _solutionContainer.TryAddSolution(startSolutionEntity.Value, elementSolution);
    }

    private void MergeFlavorProfiles(EntityUid start, EntityUid element)
    {
        if (!TryComp<FlavorProfileComponent>(start, out var startProfile))
            return;

        if (!TryComp<FlavorProfileComponent>(element, out var elementProfile))
            return;

        foreach (var flavor in elementProfile.Flavors)
        {
            if (startProfile != null && !startProfile.Flavors.Contains(flavor))
                startProfile.Flavors.Add(flavor);
        }
    }

    private void MergeTrash(Entity<EdibleComponent?> start, Entity<EdibleComponent?> element)
    {
        if (!Resolve(start, ref start.Comp, false))
            return;

        if (!Resolve(element, ref element.Comp, false))
            return;

        _ingestion.AddTrash((start, start.Comp), element.Comp.Trash);
    }

    private void MergeTags(EntityUid start, EntityUid element)
    {
        if (!TryComp<TagComponent>(element, out var elementTags))
            return;

        EnsureComp<TagComponent>(start);

        _tag.TryAddTags(start, elementTags.Tags);
    }

    private bool TryAddArbitraryEntity(Entity<FoodSequenceStartPointComponent> start, EntityUid element, EntityUid? user)
    {
        if (start.Owner == element)
            return false;

        if (start.Comp.FoodLayers.Count >= start.Comp.MaxLayers || start.Comp.Finished)
        {
            if (user is not null)
                _popup.PopupClient(Loc.GetString("food-sequence-no-space"), start, user.Value);
            return false;
        }

        var container = _container.EnsureContainer<Container>(start.Owner, "burger_entities");
        if (!_container.Insert(element, container))
            return false;

        var spriteSpec = GetEntitySpriteSpecifier(element);
        var flip = start.Comp.AllowHorizontalFlip && _random.Prob(0.5f);
        var scale = new Vector2(flip ? -0.8f : 0.8f, 0.8f);
        var layer = new FoodSequenceVisualLayer(
            "GenericIngredient",
            spriteSpec,
            scale,
            new Vector2(
                _random.NextFloat(start.Comp.MinLayerOffset.X, start.Comp.MaxLayerOffset.X),
                _random.NextFloat(start.Comp.MinLayerOffset.Y, start.Comp.MaxLayerOffset.Y))
        );

        start.Comp.FoodLayers.Add(layer);
        Dirty(start);

        UpdateFoodName(start);

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/pill_insert.ogg"), start, user);

        return true;
    }

    private SpriteSpecifier GetEntitySpriteSpecifier(EntityUid element)
    {
        if (TryComp<ItemComponent>(element, out var itemComp))
        {
            if (itemComp.StoredSprite is { } storedSprite)
                return storedSprite;

            if (!string.IsNullOrEmpty(itemComp.RsiPath))
                return new SpriteSpecifier.Rsi(new ResPath(itemComp.RsiPath), "icon");
        }

        var meta = MetaData(element);
        if (meta.EntityPrototype is { } proto)
        {
            return new SpriteSpecifier.EntityPrototype(proto.ID);
        }

        return SpriteSpecifier.Invalid;
    }

    private void UpdateFoodNameFromResult(EntityUid result)
    {
        if (!_container.TryGetContainer(result, "burger_entities", out var container) || container.ContainedEntities.Count == 0)
            return;

        List<string> customIngredientNames = new();
        foreach (var ent in container.ContainedEntities)
        {
            customIngredientNames.Add(MetaData(ent).EntityName);
        }

        if (customIngredientNames.Count == 0)
            return;

        var currentName = MetaData(result).EntityName;
        var customContent = "";
        if (customIngredientNames.Count == 1)
        {
            customContent = customIngredientNames[0];
        }
        else if (customIngredientNames.Count == 2)
        {
            customContent = $"{customIngredientNames[0]} and {customIngredientNames[1]}";
        }
        else
        {
            customContent = string.Join(", ", customIngredientNames.SkipLast(1)) + $", and {customIngredientNames.Last()}";
        }

        var newName = $"{currentName} with {customContent}";
        _metaData.SetEntityName(result, newName);
    }

    private void ConsumeContainedEntities(EntityUid food, EntityUid user)
    {
        if (!_container.TryGetContainer(food, "burger_entities", out var container))
            return;

        var entities = new List<EntityUid>(container.ContainedEntities);
        foreach (var ent in entities)
        {
            KillAndCleanUpEntity(ent, user);
        }
    }

    private void KillAndCleanUpEntity(EntityUid entity, EntityUid user)
    {
        if (TryComp<ContainerManagerComponent>(entity, out var containerManager))
        {
            foreach (var container in containerManager.Containers.Values)
            {
                var containedEntities = new List<EntityUid>(container.ContainedEntities);
                foreach (var contained in containedEntities)
                {
                    KillAndCleanUpEntity(contained, user);
                }
            }
        }

        if (HasComp<MobStateComponent>(entity))
        {
            _mobState.ChangeMobState(entity, MobState.Dead);
        }

        QueueDel(entity);
    }
}
