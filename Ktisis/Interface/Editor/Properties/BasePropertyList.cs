using Dalamud.Bindings.ImGui;

using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Scene.Entities;

namespace Ktisis.Interface.Editor.Properties;

public class BasePropertyList : ObjectPropertyList {
	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		builder.AddHeader("通用", () => this.DrawTab(entity), priority: -1);
	}

	private void DrawTab(SceneEntity entity) {
		var name = entity.Name;
		if (ImGui.InputText("名称", ref name, 100))
			entity.Name = name;
	}
}
