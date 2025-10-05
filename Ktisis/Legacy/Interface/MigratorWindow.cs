using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;

using GLib.Widgets;


using Ktisis.Interface.Types;

namespace Ktisis.Legacy.Interface;

public class MigratorWindow : KtisisWindow {
	private readonly IDalamudPluginInterface _dpi;
	private readonly LegacyMigrator _migrator;

	public MigratorWindow(
		IDalamudPluginInterface dpi,
		LegacyMigrator migrator
	) : base(
		"Ktisis 开发者预览版",
		ImGuiWindowFlags.AlwaysAutoResize
	) {
		this._dpi = dpi;
		this._migrator = migrator;
		this.ShowCloseButton = false;
		this.RespectCloseHotkey = false;
	}
	
	private readonly Stopwatch _timer = new();
	private bool _elapsed;
	
	private const uint ColorYellow = 0xFF00FFFF;
	
	private const int WaitTime = 10;

	private bool CanBegin => this._timer.Elapsed.TotalSeconds >= WaitTime || this._elapsed;

	public override void OnOpen() {
		this._timer.Reset();
		this._timer.Start();
		this._elapsed = false;
	}
	
	public override void Draw() {
		if (!this._elapsed && this.CanBegin) {
			this._timer.Stop();
			this._elapsed = true;
		}
		
		var style = ImGui.GetStyle();
		
		Icons.DrawIcon(FontAwesomeIcon.ExclamationCircle);
		ImGui.SameLine();
		ImGui.Text("您已经安装了Ktisis的开发版本。");
		
		ImGui.Spacing();
		
		ImGui.Text("此版本目前是 ");
		ImGui.SameLine(0, style.ItemInnerSpacing.X);
		using (var _ = ImRaii.PushColor(ImGuiCol.Text, ColorYellow))
			ImGui.Text("开发预览版本");
		ImGui.SameLine(0, style.ItemInnerSpacing.X);
		ImGui.Text(" - 它主要用于测试新功能。");
		
		ImGui.Text("在这个阶段只实现了最基本的功能，因此将缺少许多UI/UX。");
		
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		ImGui.SameLine();
		ImGui.Text("预期内容：");
		
		ImGui.Spacing();
		
		ImGui.Text("这不是具有完整功能的最终版本。");
		ImGui.Text(
			"以下内容将在稍后的测试中引入：\n" +
			"	• 当前版本中缺少的所有内容\n" +
			"	• 编辑武器和道具\n" +
			"	• 操作装备模型\n" +
			"	• 导入和导出灯光预设\n" +
			"	• 动画控制\n" +
			"	• 复制&粘贴\n"
		);
		
		ImGui.Spacing();
		ImGui.Text("撤消和重做当前仅对对象变换生效。");
		ImGui.Text("计划支持对对象进行编辑，例如外观更改。");
		ImGui.Spacing();
		ImGui.Text("角色外观编辑也可能与Glamourer所做的更改相冲突。");
		ImGui.Text("我希望与Glamourer的开发人员讨论如何实现IPC来解决这个问题。");
		ImGui.Spacing();
		ImGui.Text("许多配置选项也将丢失，这些选项将在测试期间陆续添加。");
		ImGui.Text("您当前的配置不会转入此版本。");
		
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		
		using (var _ = ImRaii.Disabled(!this.CanBegin && !(ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.ModShift)))) {
			var text = this.CanBegin ? "开始" : $"开始（{Math.Ceiling((decimal)WaitTime - this._timer.Elapsed.Seconds)} 秒）";
			if (ImGui.Button(text)) {
				this._migrator.Begin();
				this.Close();
			}
		}
		
		ImGui.Spacing();
	}
}
