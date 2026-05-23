#pragma once

#include "mmdplugin/mmd_plugin.h"

#define MAKE_MMD_LUA

#ifdef MAKE_MMD_LUA
# define MMD_LUA_DLL_FUNC_API __declspec(dllexport)
#else
# define MMD_LUA_DLL_FUNC_API __declspec(dllimport)
# pragma comment(lib, "MMDLua")
#endif // MAKE_MMD_UTILITY

#include "Shared.h"
#include <Windows.h>
#include <d3d9.h>
#include <atomic>
#include <functional>
#include <utility>

namespace control
{
	class Control;

	struct ReferenceCounter
	{
		MMD_LUA_DLL_FUNC_API virtual ~ReferenceCounter() = default;

		MMD_LUA_DLL_FUNC_API virtual int AddRef() const;

		MMD_LUA_DLL_FUNC_API virtual int Release() const;

	protected:
		ReferenceCounter() = default;

	private:
		mutable int ref_cnt_ = 1;
	};

	struct MenuFlag
	{
		UINT flag_;

		bool isGrayed() const;

		bool isDisabled() const;

		bool isBitmap() const;

		bool isPopUp() const;

		bool isHilite() const;

		bool isOwnerDraw() const;

		bool isSystemMenu() const;

		bool isMouseSelect() const;
	};

	class IMenu : public ReferenceCounter
	{
	public:
		enum class Type
		{
			CheckBox,
			List,
			Command,
		};

	private:
		struct Pimpl;
		Pimpl* pimpl;

	protected:
		MMD_LUA_DLL_FUNC_API IMenu(Control* ctrl);

		MMD_LUA_DLL_FUNC_API explicit IMenu(Control* ctrl, int id);

	public:
		MMD_LUA_DLL_FUNC_API virtual ~IMenu();

		MMD_LUA_DLL_FUNC_API virtual void SetType(Type t);

		MMD_LUA_DLL_FUNC_API virtual HMENU getMenuHandle() const;

		MMD_LUA_DLL_FUNC_API virtual int id() const;

		MMD_LUA_DLL_FUNC_API virtual void SetWindow(HWND hwnd, LPWSTR lpszItemName);

		MMD_LUA_DLL_FUNC_API virtual void SetWindowWithoutSubMenu(HWND hwnd, LPWSTR lpszItemName);


		MMD_LUA_DLL_FUNC_API virtual void AppendChild(MENUITEMINFOW& mii);

		MMD_LUA_DLL_FUNC_API virtual void AppendSeparator();

		MMD_LUA_DLL_FUNC_API virtual void AppendChild(LPWSTR lpszItemName, IMenu* hmenuSub);

		MMD_LUA_DLL_FUNC_API virtual void MenuSelect(HWND hwnd, UINT item_id_or_index, MenuFlag flag, HMENU hMenu) = 0;

		struct CommandArgs
		{
			HWND window_hwnd;
			UINT item_id;
			int notify_code;
			HWND control_hwnd;
		};

		MMD_LUA_DLL_FUNC_API virtual void Command(const CommandArgs& args) = 0;
	};


	struct MenuDelegate : IMenu
	{
		explicit MenuDelegate(Control* ctrl) : IMenu(ctrl) {}

		MenuDelegate(Control* ctrl, int id) : IMenu(ctrl, id) { IMenu::SetType(Type::Command); }

		void MenuSelect(HWND hwnd, UINT item_id_or_index, MenuFlag flag, HMENU hMenu) override
		{
			if (menu_select) menu_select(hwnd, item_id_or_index, flag, hMenu);
		}

		void Command(const CommandArgs& args) override
		{
			if (command) command(args);
		}

		std::function<void(const CommandArgs&)> command;
		std::function<void(HWND, UINT, MenuFlag, HMENU)> menu_select;
	};

	struct MenuCheckBox : MenuDelegate
	{
		explicit MenuCheckBox(Control* ctrl) : MenuDelegate(ctrl) { MenuDelegate::SetType(Type::CheckBox); }

		MenuCheckBox(Control* ctrl, int id) : MenuDelegate(ctrl, id) { MenuDelegate::SetType(Type::CheckBox); }

		MMD_LUA_DLL_FUNC_API virtual void check(bool is_check);

		MMD_LUA_DLL_FUNC_API virtual void reverseCheck();

		MMD_LUA_DLL_FUNC_API virtual bool isChecked() const;
	};
}

class MMDLua : public MMDPluginDLL4
{
public:
	control::IMenu* getUitilityMenu() const
	{
		top_menu_->AddRef();
		return top_menu_;
	}

	control::Control* getControl() { return ctrl_; }

	explicit MMDLua(IDirect3DDevice9* device);

	~MMDLua();

	void WndProc(CWPSTRUCT* param) override;

	void MsgProc(int code, MSG* param) override;

	std::pair<bool, LRESULT> WndProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) override;

	const char* getPluginTitle() const override { return MMD_UTILITY; }

	static constexpr const char* MMD_UTILITY = "MMDLua";

	static MMDLua* getObject()
	{
		return dynamic_cast<MMDLua*>(mmp::getDLL3Object(MMD_UTILITY));
	}

private:
	control::Control* ctrl_;
	control::IMenu* top_menu_;
	IDirect3DDevice9* device_;
	mish::DialogWindowDesc dialog_desc_;
	UINT save_menu_item_id;
	std::atomic<bool> save_project_;
	LPCWSTR ok_text_;

	void EventListener();
	void PostPresent(CONST RECT* pSourceRect, CONST RECT* pDestRect, HWND hDestWindow, CONST RGNDATA* pDirtyRegion, HRESULT& res) override;
};

MMD_LUA_DLL_FUNC_API void OpenConsole();

#ifdef NDEBUG
//#define OpenConsole() void(0)
#endif // NDEBUG
