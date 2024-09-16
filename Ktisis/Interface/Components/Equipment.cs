using ImGuiNET;

using Ktisis.Util;
using Dalamud.Interface;

namespace Ktisis.Interface.Components {

	public class Equipment {
		public static void CreateGlamourQuestionPopup()
		{
			GuiHelpers.PopupConfirm(
				"##popup_glamour_plate_use##1",
				() => {
					ImGui.Text("每一次投影模板窗口关闭时，\n投影模板记录都会更新。\n\n要立即填充记录，请打开共通技能中的\"投影模板\"（角色窗口那里的也可以）,\n然后关闭它。\n使用这项技能需要你在休息区。\n\n它也可以用聊天命令调出来：");
					ImGui.Text("/ac \"Glamour Plate\"");
					ImGui.SameLine();
					if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Clipboard, "点这里复制这条命令： \"ac 投影模板\"。"))
						ImGui.SetClipboardText("/ac 投影模板");
				},
				null,
				true);
		}
		public static void OpenGlamourQuestionPopup()
		{
			ImGui.OpenPopup("##popup_glamour_plate_use##1");
		}
	}
}
