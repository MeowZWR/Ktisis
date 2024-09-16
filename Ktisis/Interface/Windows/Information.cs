using System;
using System.Numerics;

using ImGuiNET;

using Ktisis.Helpers;

namespace Ktisis.Interface.Windows {
	internal static class Information {
		public static bool Visible = false;

		public static void Show() => Visible = true;
		public static void Toggle() => Visible = !Visible;

		private static Vector2 ButtonSize = new Vector2(0, 25);
		private static Vector4 DiscordColor = new Vector4(86, 98, 246, 255) / 255;
		private static Vector4 KofiColor = new Vector4(255, 91, 94, 255) / 255;

		public static void Draw() {
			if (!Visible) return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin($"关于Ktisis ({Ktisis.Version})", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)) {
				ImGui.BeginGroup();

				ImGui.Text("感谢您安装Ktisis！");

				ImGui.Spacing();
				ImGui.Text("此插件仍处于早期的alpha版本，因此你可能会遇到一些问题和bug。");
				ImGui.Text("因此，建议您定期保存进度。");
				ImGui.Text("请随时打开Discord，在issues或suggestions频道中向我们反馈。");

				ImGui.Spacing();
				ImGui.Text("您可能需要花费一些时间来熟悉这些设置。");
				ImGui.Text("准备好后，在聊天框输入'/ktisis'并计入集体动作模式即可开始。");

				ImGui.EndGroup();
				ImGui.SameLine(ImGui.GetItemRectSize().X + 50);
				ImGui.BeginGroup();

				ImGui.PushStyleColor(ImGuiCol.Button, DiscordColor);
				if (ImGui.Button("加入我们的Discord", ButtonSize))
					Common.OpenBrowser("https://discord.gg/ktisis");
				ImGui.PopStyleColor();

				ImGui.PushStyleColor(ImGuiCol.Button, KofiColor);
				if (ImGui.Button("来Ko-fi支持我们", ButtonSize))
					Common.OpenBrowser("https://ko-fi.com/chirpxiv");
				ImGui.PopStyleColor();

				if (ImGui.Button("GitHub", ButtonSize))
					Common.OpenBrowser("https://github.com/ktisis-tools/Ktisis/");

				ImGui.Spacing();

				if (ImGui.Button("打开设置窗口", ButtonSize))
					ConfigGui.Show();

				if (ImGui.Button("开始捏姿势吧", ButtonSize))
					Workspace.Workspace.Show();

				ImGui.EndGroup();

				ButtonSize.X = Math.Max(ImGui.GetItemRectSize().X, ButtonSize.X);
			}

			ImGui.PopStyleVar();
			ImGui.End();
		}
	}
}
