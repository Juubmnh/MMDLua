#include "pch.h"
#include "mmd_lua.h"
#include "mmdplugin/mmd_plugin.h"
#include "Shared.h"
#include <Windows.h>
#include <d3d9.h>
#include <cassert>
#include <cstdio>
#include <stdexcept>
#include <string>
#include <thread>
#include <unordered_map>
#include <utility>
#include <vector>
#include <fstream>

#ifdef NDEBUG
#define printf(...) (void)0
#define printf_s(...) (void)0
#endif // NDEBUG

namespace control
{
	int ReferenceCounter::AddRef() const
	{
		ref_cnt_++;
		return ref_cnt_;
	}

	int ReferenceCounter::Release() const
	{
		if (--ref_cnt_ == 0)
		{
			delete this;
			return 0;
		}
		return ref_cnt_;
	}

	bool MenuFlag::isGrayed() const { return (flag_ & MF_GRAYED) != 0; }

	bool MenuFlag::isDisabled() const { return (flag_ & MF_DISABLED) != 0; }

	bool MenuFlag::isBitmap() const { return (flag_ & MF_BITMAP) != 0; }

	bool MenuFlag::isPopUp() const { return (flag_ & MF_POPUP) != 0; }

	bool MenuFlag::isHilite() const { return (flag_ & MF_HILITE) != 0; }

	bool MenuFlag::isOwnerDraw() const { return (flag_ & MF_OWNERDRAW) != 0; }

	bool MenuFlag::isSystemMenu() const { return (flag_ & MF_SYSMENU) != 0; }

	bool MenuFlag::isMouseSelect() const { return (flag_ & MF_MOUSESELECT) != 0; }

	struct IMenu::Pimpl
	{
		int id_;
		HMENU menu_handle_ = ::CreateMenu();
		std::vector<IMenu*> child_menu_;
		HWND window_handle_ = nullptr;
		Type type;
	};

	class Control
	{
		using ID = int;
		std::unordered_map<ID, IMenu*> menu_;
		int menu_select_id_ = -1;
		Control(const Control&) = delete;
		void operator=(const Control&) = delete;
	public:
		Control() = default;

		void AddObj(IMenu* menu);

		void WndProc(int code, const MSG* param);
	};

	IMenu::IMenu(Control* ctrl) : IMenu(ctrl, createWM_APP_ID()) {}

	IMenu::IMenu(Control* ctrl, int id)
	{
		pimpl = new Pimpl();
		pimpl->id_ = id;
		pimpl->type = Type::List;
		ctrl->AddObj(this);
	}

	void IMenu::SetType(Type t)
	{
		pimpl->type = t;
	}

	HMENU IMenu::getMenuHandle() const { return pimpl->menu_handle_; }

	int IMenu::id() const { return pimpl->id_; }

	void IMenu::SetWindow(HWND hwnd, LPWSTR lpszItemName)
	{
		pimpl->window_handle_ = hwnd;
		auto hmenu = GetMenu(hwnd);
		MENUITEMINFOW mii{};

		mii.cbSize = sizeof(MENUITEMINFOW);
		mii.fMask = MIIM_ID | MIIM_TYPE;
		mii.wID = pimpl->id_;
		mii.fType = MFT_STRING;
		mii.dwTypeData = lpszItemName;
		mii.hSubMenu = pimpl->menu_handle_;
		mii.fMask |= MIIM_SUBMENU;

		InsertMenuItemW(hmenu, mii.wID, FALSE, &mii);
	}

	void IMenu::SetWindowWithoutSubMenu(HWND hwnd, LPWSTR lpszItemName)
	{
		pimpl->window_handle_ = hwnd;
		auto hmenu = GetMenu(hwnd);
		MENUITEMINFOW mii{};

		mii.cbSize = sizeof(MENUITEMINFOW);
		mii.fMask = MIIM_ID | MIIM_TYPE;
		mii.wID = pimpl->id_;
		mii.fType = MFT_STRING;
		mii.dwTypeData = lpszItemName;

		InsertMenuItemW(hmenu, mii.wID, FALSE, &mii);
	}

	IMenu::~IMenu()
	{
		delete pimpl;
	}

	void IMenu::AppendChild(MENUITEMINFOW& mii)
	{
		InsertMenuItemW(pimpl->menu_handle_, 0, FALSE, &mii);
	}

	void IMenu::AppendSeparator()
	{
		InsertMenuW(pimpl->menu_handle_, 0xffffffff, MF_BYPOSITION | MF_SEPARATOR, 0, nullptr);
	}

	void IMenu::AppendChild(LPWSTR lpszItemName, IMenu* hmenuSub)
	{
		assert(lpszItemName != NULL);
		MENUITEMINFOW mii{};

		mii.cbSize = sizeof(MENUITEMINFOW);
		mii.fMask = MIIM_ID | MIIM_TYPE;

		mii.fType = MFT_STRING;
		mii.dwTypeData = lpszItemName;

		if (hmenuSub != nullptr)
		{
			hmenuSub->AddRef();
			mii.wID = hmenuSub->id();
			pimpl->child_menu_.push_back(hmenuSub);
			switch (hmenuSub->pimpl->type)
			{
			case Type::List:
				mii.fMask |= MIIM_SUBMENU;
				mii.hSubMenu = hmenuSub->getMenuHandle();
				break;
			case Type::Command:
				mii.fType |= MFT_RADIOCHECK;
				break;
			case Type::CheckBox:

				break;
			}
		}
		else
		{
			mii.wID = createWM_APP_ID();
		}

		InsertMenuItemW(pimpl->menu_handle_, mii.wID, FALSE, &mii);
	}

	void MenuCheckBox::check(bool is_check)
	{
		CheckMenuItem(GetMenu(getHWND()), id(), MF_BYCOMMAND | (is_check ? MF_CHECKED : MF_UNCHECKED));
	}

	void MenuCheckBox::reverseCheck() { check(!isChecked()); }

	bool MenuCheckBox::isChecked() const
	{
		auto uState = GetMenuState(GetMenu(getHWND()), id(), MF_BYCOMMAND);
		return (uState & MFS_CHECKED) != 0;
	}

	void Control::AddObj(IMenu* menu)
	{
		if (menu_.insert({ menu->id(), menu }).second == false) throw std::runtime_error("すでにIDが使われています。");
	}

	void Control::WndProc(int code, const MSG* param)
	{
		//menu_select_id_ = -1;
		switch (param->message)
		{
		case WM_LBUTTONUP:
		{
			auto it = menu_.find(menu_select_id_);
			if (it != menu_.end())
			{
				IMenu::CommandArgs args{};
				args.window_hwnd = param->hwnd;
				args.item_id = LOWORD(param->wParam);
				args.notify_code = HIWORD(param->wParam);
				args.control_hwnd = (HWND)param->lParam;
				it->second->Command(args);
			}
			break;
		}
		case WM_MENUSELECT:
		{
			menu_select_id_ = LOWORD(param->wParam);
			auto it = menu_.find(menu_select_id_);
			if (it != menu_.end())
			{
				it->second->MenuSelect(param->hwnd, LOWORD(param->wParam), { HIWORD(param->wParam) }, (HMENU)param->lParam);
			}
			break;
		}
		default:
			break;
		}
	}
}

void MMDLua::EventListener()
{
	auto hWnd = getHWND();
	auto main_data = mmp::getMMDMainData();

	HANDLE hPipeEvent = CreateEventW(nullptr, FALSE, FALSE, L"Global\\MMDLuaPipeListenerEvent");
	if (!hPipeEvent) return;

	HANDLE hSaveEvent = CreateEventW(nullptr, FALSE, FALSE, L"Global\\MMDLuaSaveProjectEvent");
	if (!hSaveEvent) return;

	HANDLE hDropFileEvent = CreateEventW(nullptr, FALSE, FALSE, L"Global\\MMDLuaDropFileEvent");
	if (!hDropFileEvent) return;

	HANDLE hEvents[] = { hPipeEvent, hSaveEvent, hDropFileEvent };

	while (true)
	{
		DWORD result = WaitForMultipleObjects(ARRAYSIZE(hEvents), hEvents, FALSE, INFINITE);
		if (result == WAIT_OBJECT_0)
		{
			printf_s("On MMDLuaPipeListenerEvent\n");
			mish::SendPipeData(nullptr, main_data, mish::PIPE_DATA_TYPE::SEND_MAIN_DATA);
		}
		else if (result == WAIT_OBJECT_0 + 1)
		{
			printf_s("On MMDLuaSaveProjectEvent\n");
			save_project_.store(true);
		}
		else if (result == WAIT_OBJECT_0 + 2)
		{
			std::wstring dirPath{};
			if (!mish::GetMMDDirectory(hWnd, dirPath))
			{
				continue;
			}

			printf_s("On MMDLuaDropFileEvent\n");
			auto path = dirPath + L"\\MMDLua\\motion.vmd";
			if (std::ifstream(path).good())
			{
				mish::StimulateDropFile(hWnd, path, ok_text_);
			}
		}
	}
}

void MMDLua::PostPresent(const RECT* pSourceRect, const RECT* pDestRect, HWND hDestWindow, const RGNDATA* pDirtyRegion, HRESULT& res)
{
	if (save_project_.load())
	{
		mish::SaveProject(getHWND(), save_menu_item_id);
		save_project_.store(false);
	}
}

MMDLua::MMDLua(IDirect3DDevice9* device) : device_(device), dialog_desc_()
{
	auto hwnd = getHWND();
	printf_s("MMD HWnd: %p\n", hwnd);
	ctrl_ = new control::Control();
	auto menu = new control::MenuCheckBox(ctrl_);
	menu->SetWindowWithoutSubMenu(hwnd, const_cast<LPWSTR>(UTF8ToWString(std::string(MMD_UTILITY)).c_str()));
	menu->SetType(control::IMenu::Type::Command);
	menu->command = [this, hwnd](const control::IMenu::CommandArgs& args)
		{
			if (dialog_desc_.enumState) return;

			if (dialog_desc_.hWnd)
			{
				printf_s("Dialog window exists: %p\n", dialog_desc_.hWnd);
				HANDLE hProcess = OpenProcess(
					SYNCHRONIZE | PROCESS_TERMINATE,
					FALSE,
					dialog_desc_.pid
				);
				if (hProcess)
				{
					auto currentThreadId = GetCurrentThreadId();
					auto targetThreadId = GetWindowThreadProcessId(dialog_desc_.hWnd, nullptr);
					AttachThreadInput(currentThreadId, targetThreadId, TRUE);

					ShowWindow(dialog_desc_.hWnd, SW_SHOW);
					if (SetForegroundWindow(dialog_desc_.hWnd) && SetFocus(dialog_desc_.hWnd))
					{
						printf_s("Activating dialog succeeded\n");
						return;
					}
					else
					{
						printf_s("Terminate dialog process\n");
						TerminateProcess(hProcess, 0);
					}

					AttachThreadInput(currentThreadId, targetThreadId, FALSE);
					CloseHandle(hProcess);
				}
			}
			
			std::wstring dirPath{};
			if (!mish::GetMMDDirectory(hwnd, dirPath))
			{
				return;
			}

			STARTUPINFOW si = { sizeof(STARTUPINFOW) };
			PROCESS_INFORMATION pi{};
			if (CreateProcessW(
				(dirPath + L"\\MMDLua\\Dialog.exe").c_str(),
				nullptr,
				nullptr,
				nullptr,
				FALSE,
				NULL,
				nullptr,
				(dirPath + L"\\MMDLua").c_str(),
				&si,
				&pi))
			{
				printf_s("Created process id: %d\n", pi.dwProcessId);
				dialog_desc_ = {};
				dialog_desc_.pid = pi.dwProcessId;
				std::thread(mish::EnumWindowsProc, &dialog_desc_).detach();

				CloseHandle(pi.hProcess);
				CloseHandle(pi.hThread);
			}
		};
	top_menu_ = menu;
	DrawMenuBar(hwnd);

	HMENU hMenu = GetMenu(hwnd);
	mish::TraversalMenuItems(hMenu);
	HMENU hFileMenu = GetSubMenu(hMenu, 0);
	mish::TraversalMenuItems(hFileMenu);
	save_menu_item_id = GetMenuItemID(hFileMenu, 2);

	ok_text_ = mish::GetSystemOKText();
	std::thread(&MMDLua::EventListener, this).detach();
}

MMDLua::~MMDLua()
{
	delete ctrl_, top_menu_;
}

void MMDLua::WndProc(CWPSTRUCT* param)
{

}

void MMDLua::MsgProc(int code, MSG* param)
{
	ctrl_->WndProc(code, param);
}

std::pair<bool, LRESULT> MMDLua::WndProc(HWND, UINT, WPARAM, LPARAM) { return { false, 0 }; }

#ifndef NDEBUG
FILE* in, * out;
void OpenConsole()
{
	AllocConsole();
	freopen_s(&out, "CONOUT$", "w", stdout);
	freopen_s(&in, "CONIN$", "r", stdin);
}
#endif // !NDEBUG

int version() { return 4; }

MMDPluginDLL4* create4(IDirect3DDevice9* device)
{
#ifndef NDEBUG
	OpenConsole();
#endif // !NDEBUG

	mmdutl::f_wsopen_s.hook("MSVCR90.DLL", "_wsopen_s", mmdutl::Mywsopen_s);
	mmdutl::f_write.hook("MSVCR90.DLL", "_write", mmdutl::MyWrite);
	mmdutl::f_close.hook("MSVCR90.DLL", "_close", mmdutl::Myclose);

	return new MMDLua(device);
}

void destroy4(MMDPluginDLL4* p)
{
	delete p;

	mmdutl::f_wsopen_s.reset();
	mmdutl::f_write.reset();
	mmdutl::f_close.reset();

#ifndef NDEBUG
	fclose(in);
	fclose(out);
#endif // !NDEBUG
}
