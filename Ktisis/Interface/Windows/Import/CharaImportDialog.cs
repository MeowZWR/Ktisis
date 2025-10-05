using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Chara;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Windows.Import;

public class CharaImportDialog : EntityEditWindow<ActorEntity> {
	private readonly IEditorContext _ctx;
	
	private readonly CharaImportUI _import;

	public CharaImportDialog(
		IEditorContext ctx,
		CharaImportUI import
	) : base(
		"导入外观",
		ctx,
		ImGuiWindowFlags.AlwaysAutoResize
	) {
		this._ctx = ctx;
		this._import = import;
		this._import.Context = ctx;
		this._import.OnNpcSelected += this.OnNpcSelected;
	}
	
	// Events

	public void SetMethod(LoadMethod method) {
		this._import.Method = method;
	}
	
	private void OnNpcSelected(CharaImportUI sender) => sender.ApplyTo(this.Target);
	
	// Draw UI
	
	public override void Draw() {
		this.UpdateTarget();
		
		ImGui.Text($"正在为 {this.Target.Name} 导入外观");
		ImGui.Spacing();

		this.DrawEmbed();
	}

	public void DrawEmbed() {
		this.PreDraw();
		this._import.DrawLoadMethods();
		ImGui.Spacing();
		this._import.DrawImport();
		ImGui.Spacing();
		this.DrawCharaApplication();
	}

	private void DrawCharaApplication() {
		this._import.DrawModesSelect();
		
		ImGui.Spacing();
		ImGui.Spacing();
		
		using var _ = ImRaii.Disabled(!this._import.HasSelection);
		if (ImGui.Button("应用"))
			this._import.ApplyTo(this.Target);
		
		ImGui.Spacing();
	}
}
