using GuiToolkit.AssetHandling;
using UnityEngine;

/// <summary>
/// Factory for creating <see cref="AddressablesAssetProvider"/> instances.
/// Can be added to the project via the Unity "Create" menu.
/// </summary>
[CreateAssetMenu(
    fileName = nameof(AddressablesProviderFactory),
    menuName = "AddressablesProviderFactory")]
public class AddressablesProviderFactory : AbstractAssetProviderFactory
{
    /// <summary>
    /// Creates a new <see cref="AddressablesAssetProvider"/> instance.
    /// </summary>
    public override IAssetProvider CreateProvider() => new AddressablesAssetProvider();
}
