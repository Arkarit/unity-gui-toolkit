using GuiToolkit;
using GuiToolkit.AssetHandling;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(AddressablesProviderFactory), menuName = "AddressablesProviderFactory")]
public class AddressablesProviderFactory : AbstractAssetProviderFactory
{
	public override IAssetProvider CreateProvider() => new AddressablesProvider();
}
