#pragma once

#include "mmdplugin/mmd_plugin.h"
#include <Windows.h>
#include <atomic>
#include <chrono>
#include <codecvt>
#include <corecrt.h>
#include <cstdio>
#include <fcntl.h>
#include <fstream>
#include <io.h>
#include <locale>
#include <ratio>
#include <ShlObj_core.h>
#include <string>
#include <string.h>
#include <thread>
#include <utility>
#include <wchar.h>

inline std::string WStringToUTF8(const std::wstring& wstr)
{
    return std::wstring_convert<std::codecvt_utf8<wchar_t>>().to_bytes(wstr);
}

inline std::wstring UTF8ToWString(const std::string& str)
{
    return std::wstring_convert<std::codecvt_utf8<wchar_t>>().from_bytes(str);
}

#ifndef NDEBUG
DWORD bytesWrittenTotal;
#endif // !NDEBUG

template<typename T>
static DWORD PipeWrite(HANDLE hPipe, const T& data)
{
	DWORD bytesWritten = NULL;
	WriteFile(hPipe, &data, sizeof(data), &bytesWritten, nullptr);
#ifndef NDEBUG
	bytesWrittenTotal += bytesWritten;
#endif // !NDEBUG
	return bytesWritten;
}

template<typename T>
static DWORD PipeWriteArray(HANDLE hPipe, const T* pData, DWORD count)
{
	DWORD bytesWritten = NULL;
	WriteFile(hPipe, pData, sizeof(T) * count, &bytesWritten, nullptr);
#ifndef NDEBUG
	bytesWrittenTotal += bytesWritten;
#endif // !NDEBUG
	return bytesWritten;
}

namespace mish
{
	BOOL GetMMDDirectory(HWND hWnd, std::wstring& path)
	{
		DWORD pid;
		GetWindowThreadProcessId(hWnd, &pid);

		HANDLE hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, pid);
		if (!hProcess) return FALSE;

		wchar_t buffer[MAX_PATH];
		DWORD size = MAX_PATH;
		if (!QueryFullProcessImageNameW(hProcess, NULL, buffer, &size))
		{
			CloseHandle(hProcess);
			return FALSE;
		}
		CloseHandle(hProcess);

		std::wstring mmd_path(buffer);
		path = mmd_path.substr(0, mmd_path.find_last_of(L"\\/"));
		return TRUE;
	}

	enum class PIPE_DATA_TYPE
	{
		SEND_HWND,
		SEND_MAIN_DATA
	};

	static void SendPipeData(HWND hWnd, mmp::MMDMainData* main_data, PIPE_DATA_TYPE pipe_data_type)
	{
		HANDLE hPipe = CreateFileW(
			L"\\\\.\\pipe\\MMDLuaPipe",
			GENERIC_WRITE,
			NULL,
			nullptr,
			OPEN_EXISTING,
			NULL,
			nullptr
		);
		if (hPipe == INVALID_HANDLE_VALUE) return;

#ifndef NDEBUG
		bytesWrittenTotal = NULL;
#endif // !NDEBUG

		if (pipe_data_type == PIPE_DATA_TYPE::SEND_HWND)
		{
			PipeWrite(hPipe, 0);
			printf_s("Writing MMD hWnd (%d)\n", bytesWrittenTotal);
			PipeWrite(hPipe, hWnd);
			printf_s("Writing finished (%d)\n", bytesWrittenTotal);
		}
		else if (pipe_data_type == PIPE_DATA_TYPE::SEND_MAIN_DATA)
		{
			PipeWrite(hPipe, 1);
			printf_s("Writing mouse positions (%d)\n", bytesWrittenTotal);
			PipeWrite(hPipe, main_data->mouse_x);
			PipeWrite(hPipe, main_data->mouse_y);
			PipeWrite(hPipe, main_data->pre_mouse_x);
			PipeWrite(hPipe, main_data->pre_mouse_y);

			printf_s("Writing key states (%d)\n", bytesWrittenTotal);
			PipeWrite(hPipe, main_data->key_up);
			PipeWrite(hPipe, main_data->key_down);
			PipeWrite(hPipe, main_data->key_left);
			PipeWrite(hPipe, main_data->key_right);
			PipeWrite(hPipe, main_data->key_shift);
			PipeWrite(hPipe, main_data->key_space);
			PipeWrite(hPipe, main_data->key_f9);
			PipeWrite(hPipe, main_data->key_x_or_f11);
			PipeWrite(hPipe, main_data->key_z);
			PipeWrite(hPipe, main_data->key_c);
			PipeWrite(hPipe, main_data->key_v);
			PipeWrite(hPipe, main_data->key_d);
			PipeWrite(hPipe, main_data->key_a);
			PipeWrite(hPipe, main_data->key_b);
			PipeWrite(hPipe, main_data->key_g);
			PipeWrite(hPipe, main_data->key_s);
			PipeWrite(hPipe, main_data->key_i);
			PipeWrite(hPipe, main_data->key_h);
			PipeWrite(hPipe, main_data->key_k);
			PipeWrite(hPipe, main_data->key_p);
			PipeWrite(hPipe, main_data->key_u);
			PipeWrite(hPipe, main_data->key_j);
			PipeWrite(hPipe, main_data->key_f);
			PipeWrite(hPipe, main_data->key_r);
			PipeWrite(hPipe, main_data->key_l);
			PipeWrite(hPipe, main_data->key_close_bracket);
			PipeWrite(hPipe, main_data->key_backslash);
			PipeWrite(hPipe, main_data->key_tab);
			PipeWrite(hPipe, main_data->key_enter);
			PipeWrite(hPipe, main_data->key_ctrl);
			PipeWrite(hPipe, main_data->key_alt);

			printf_s("Writing camera transformation (%d)\n", bytesWrittenTotal);
			PipeWrite(hPipe, main_data->xyz);
			PipeWrite(hPipe, main_data->rxyz);

			printf_s("Writing counters (%d)\n", bytesWrittenTotal);
			PipeWrite(hPipe, main_data->counter);
			PipeWrite(hPipe, main_data->counter_f);

			printf_s("Writing camera keyframes (%d)\n", bytesWrittenTotal);
			for (int i = 0; i < 10000; i++)
			{
				PipeWrite(hPipe, main_data->camera_key_frame[i].frame_no);
				PipeWrite(hPipe, main_data->camera_key_frame[i].pre_index);
				PipeWrite(hPipe, main_data->camera_key_frame[i].next_index);
				PipeWrite(hPipe, main_data->camera_key_frame[i].length);
				PipeWrite(hPipe, main_data->camera_key_frame[i].xyz);
				PipeWrite(hPipe, main_data->camera_key_frame[i].rxyz);
				PipeWriteArray(hPipe, main_data->camera_key_frame[i].hokan1_x, 6);
				PipeWriteArray(hPipe, main_data->camera_key_frame[i].hokan1_y, 6);
				PipeWriteArray(hPipe, main_data->camera_key_frame[i].hokan2_x, 6);
				PipeWriteArray(hPipe, main_data->camera_key_frame[i].hokan2_y, 6);
				PipeWrite(hPipe, main_data->camera_key_frame[i].is_perspective);
				PipeWrite(hPipe, main_data->camera_key_frame[i].view_angle);
				PipeWrite(hPipe, main_data->camera_key_frame[i].is_selected);
				PipeWrite(hPipe, main_data->camera_key_frame[i].looking_model_index);
				PipeWrite(hPipe, main_data->camera_key_frame[i].looking_bone_index);
			}

			int count = 0;
			for (int i = 0; i < 255; i++)
			{
				if (main_data->model_data[i]) count++;
			}
			printf_s("Model data count: %d\n", count);
			PipeWrite(hPipe, count);
			for (int i = 0; i < 255; i++)
			{
				if (!main_data->model_data[i]) continue;

				PipeWriteArray(hPipe, main_data->model_data[i]->name_jp, 50);
				PipeWriteArray(hPipe, main_data->model_data[i]->name_en, 50);
				PipeWriteArray(hPipe, main_data->model_data[i]->comment_jp, 256);
				PipeWriteArray(hPipe, main_data->model_data[i]->comment_en, 292);
				PipeWriteArray(hPipe, main_data->model_data[i]->file_path, 256);

				PipeWrite(hPipe, main_data->model_data[i]->bone_count);
				PipeWrite(hPipe, main_data->model_data[i]->morph_count);
				PipeWrite(hPipe, main_data->model_data[i]->ik_count);

				printf_s("Model data [%d] bone current keyframe count: %d\n", i, main_data->model_data[i]->bone_count);
				for (int j = 0; j < main_data->model_data[i]->bone_count; j++)
				{
					PipeWriteArray(hPipe, main_data->model_data[i]->bone_current_data[j].name_jp, 20);
					PipeWriteArray(hPipe, main_data->model_data[i]->bone_current_data[j].name_en, 20);
					PipeWrite(hPipe, main_data->model_data[i]->bone_current_data[j].init_x);
					PipeWrite(hPipe, main_data->model_data[i]->bone_current_data[j].init_y);
					PipeWrite(hPipe, main_data->model_data[i]->bone_current_data[j].init_z);
					PipeWrite(hPipe, main_data->model_data[i]->bone_current_data[j].x);
					PipeWrite(hPipe, main_data->model_data[i]->bone_current_data[j].y);
					PipeWrite(hPipe, main_data->model_data[i]->bone_current_data[j].z);
					PipeWrite(hPipe, main_data->model_data[i]->bone_current_data[j].x2);
					PipeWrite(hPipe, main_data->model_data[i]->bone_current_data[j].y2);
					PipeWrite(hPipe, main_data->model_data[i]->bone_current_data[j].z2);
				}

				PipeWrite(hPipe, main_data->model_data[i]->keyframe_editor_toplevel_rows);
				printf_s("Writing model keyframes (%d)\n", bytesWrittenTotal);

				{
					mmp::MMDModelData::BoneKeyFrame* current_keyframe = nullptr;
					int count = 0;
					do
					{
						current_keyframe = main_data->model_data[i]->bone_keyframe + (current_keyframe ? current_keyframe->next_index : 0);
						if (current_keyframe) count++;
					} while (current_keyframe->next_index);
					printf_s("Model data [%d] bone keyframe count: %d\n", i, count);
					PipeWrite(hPipe, count);
					printf_s("Writing (%d)\n", bytesWrittenTotal);

					current_keyframe = nullptr;
					do
					{
						current_keyframe = main_data->model_data[i]->bone_keyframe + (current_keyframe ? current_keyframe->next_index : 0);
						if (current_keyframe)
						{
							PipeWrite(hPipe, current_keyframe->frame_number);
							PipeWrite(hPipe, current_keyframe->pre_index);
							PipeWrite(hPipe, current_keyframe->next_index);

							PipeWriteArray(hPipe, current_keyframe->interpolation_curve_x1, 4);
							PipeWriteArray(hPipe, current_keyframe->interpolation_curve_y1, 4);
							PipeWriteArray(hPipe, current_keyframe->interpolation_curve_x2, 4);
							PipeWriteArray(hPipe, current_keyframe->interpolation_curve_y2, 4);

							PipeWrite(hPipe, current_keyframe->x);
							PipeWrite(hPipe, current_keyframe->y);
							PipeWrite(hPipe, current_keyframe->z);

							PipeWriteArray(hPipe, current_keyframe->rotation_q, 4);
						}
					} while (current_keyframe->next_index);
				}

				{
					mmp::MMDModelData::MorphKeyFrame* current_keyframe = nullptr;
					int count = 0;
					do
					{
						current_keyframe = main_data->model_data[i]->morph_keyframe + (current_keyframe ? current_keyframe->next_index : 0);
						if (current_keyframe) count++;
					} while (current_keyframe->next_index);
					printf_s("Model data [%d] morph keyframe count: %d\n", i, count);
					PipeWrite(hPipe, count);
					printf_s("Writing (%d)\n", bytesWrittenTotal);

					current_keyframe = nullptr;
					do
					{
						current_keyframe = main_data->model_data[i]->morph_keyframe + (current_keyframe ? current_keyframe->next_index : 0);
						if (current_keyframe)
						{
							PipeWrite(hPipe, current_keyframe->frame_number);
							PipeWrite(hPipe, current_keyframe->pre_index);
							PipeWrite(hPipe, current_keyframe->next_index);
							PipeWrite(hPipe, current_keyframe->value);
							PipeWrite(hPipe, current_keyframe->is_selected);
						}
					} while (current_keyframe->next_index);
				}

				{
					mmp::MMDModelData::ConfigurationKeyFrame* current_keyframe = nullptr;
					int count = 0;
					do
					{
						current_keyframe = main_data->model_data[i]->configuration_keyframe + (current_keyframe ? current_keyframe->next_index : 0);
						if (current_keyframe) count++;
					} while (current_keyframe->next_index);
					printf_s("Model data [%d] configuration keyframe count: %d\n", i, count);
					PipeWrite(hPipe, count);
					printf_s("Writing (%d)\n", bytesWrittenTotal);

					current_keyframe = nullptr;
					do
					{
						current_keyframe = main_data->model_data[i]->configuration_keyframe + (current_keyframe ? current_keyframe->next_index : 0);
						if (current_keyframe)
						{
							PipeWrite(hPipe, current_keyframe->frame_number);
							PipeWrite(hPipe, current_keyframe->pre_index);
							PipeWrite(hPipe, current_keyframe->next_index);
							PipeWrite(hPipe, current_keyframe->is_visible);
							PipeWriteArray(hPipe, current_keyframe->is_ik_enabled, main_data->model_data[i]->ik_count);
							PipeWriteArray(hPipe, current_keyframe->relation_setting, main_data->model_data[i]->ik_count);
						}
					} while (current_keyframe->next_index);
				}

				PipeWrite(hPipe, main_data->model_data[i]->render_order);
				PipeWrite(hPipe, main_data->model_data[i]->is_visible);
				PipeWrite(hPipe, main_data->model_data[i]->selected_bone);
				PipeWriteArray(hPipe, main_data->model_data[i]->selected_morph_indices, 4);
				PipeWrite(hPipe, main_data->model_data[i]->vscroll);
				PipeWrite(hPipe, main_data->model_data[i]->last_frame_number);
				PipeWrite(hPipe, main_data->model_data[i]->parentable_bone_count);

				printf_s("Writing single model data finished (%d)\n", bytesWrittenTotal);
			}

			printf_s("Writing others (%d)\n", bytesWrittenTotal);
			PipeWrite(hPipe, main_data->select_model);
			PipeWrite(hPipe, main_data->select_bone_type);

			PipeWrite(hPipe, main_data->mouse_over_move);

			PipeWrite(hPipe, main_data->left_frame);
			PipeWrite(hPipe, main_data->pre_left_frame);
			PipeWrite(hPipe, main_data->now_frame);

			PipeWriteArray(hPipe, main_data->edit_interpolation_curve, 4);

			PipeWrite(hPipe, main_data->is_camera_select);
			PipeWriteArray(hPipe, main_data->is_model_bone_select, 127);

			PipeWrite(hPipe, main_data->output_size_x);
			PipeWrite(hPipe, main_data->output_size_y);

			PipeWrite(hPipe, main_data->length);

			PipeWriteArray(hPipe, main_data->pmm_path, 256);

			printf_s("Writing finished (%d)\n", bytesWrittenTotal);
		}

		FlushFileBuffers(hPipe);
		CloseHandle(hPipe);
	}

	struct DialogWindowDesc
	{
		DWORD pid;
		HWND hWnd;
		bool enumState;
	};

	void EnumWindowsProc(DialogWindowDesc* desc)
	{
		desc->enumState = true;
		auto retry_count = 0;
		while (!desc->hWnd && retry_count < 100)
		{
			EnumWindows([](HWND hWnd, LPARAM lParam) -> BOOL
				{
					auto desc = reinterpret_cast<DialogWindowDesc*>(lParam);
					DWORD pid{};
					GetWindowThreadProcessId(hWnd, &pid);
					if (pid == desc->pid)
					{
						printf_s("Dialog process found: %d\n", pid);
						desc->hWnd = hWnd;
						SendPipeData(getHWND(), nullptr, PIPE_DATA_TYPE::SEND_HWND);
						return FALSE;
					}
					return TRUE;
				}, reinterpret_cast<LPARAM>(desc));

			if (desc->hWnd) break;

			retry_count++;
			Sleep(100);
		}
		desc->enumState = false;
	}

	static LPCWSTR GetSystemOKText()
	{
		auto hUser32 = GetModuleHandleW(L"user32.dll");
		if (!hUser32) hUser32 = LoadLibraryW(L"user32.dll");

		wchar_t buffer[MAX_PATH];
		auto result = LoadStringW(hUser32, 800, buffer, MAX_PATH);
		if (result > 0)
		{
			return buffer;
		}

		return L"OK";
	}

	static BOOL CALLBACK EnumChildProc(HWND hWnd, LPARAM lParam)
	{
		wchar_t className[MAX_PATH];
		wchar_t windowText[MAX_PATH];
		GetClassNameW(hWnd, className, MAX_PATH);
		GetWindowTextW(hWnd, windowText, MAX_PATH);

		if (wcscmp(className, L"Button") == 0 && std::wstring(reinterpret_cast<LPCWSTR>(lParam)).find(windowText) != std::wstring::npos)
		{
			SendMessageW(hWnd, BM_CLICK, NULL, NULL);
			return FALSE;
		}

		return TRUE;
	}

	static BOOL StimulateDropFile(HWND hWnd, std::wstring filePath, LPCWSTR systemOKText)
	{
		auto bufferSize = sizeof(DROPFILES) + (filePath.size() + 2) * sizeof(wchar_t);

		auto hGlobal = GlobalAlloc(GHND, bufferSize);
		if (!hGlobal) return FALSE;

		auto pDrop = static_cast<DROPFILES*>(GlobalLock(hGlobal));
		if (!pDrop) return FALSE;

		ZeroMemory(pDrop, bufferSize);
		pDrop->pFiles = sizeof(DROPFILES);
		pDrop->fWide = TRUE;

		auto pFilePath = reinterpret_cast<LPWSTR>(reinterpret_cast<BYTE*>(pDrop) + sizeof(DROPFILES));
		wcscpy_s(pFilePath, filePath.size() + 1, filePath.c_str());
		*(pFilePath + filePath.size() + 1) = L'\0';

		GlobalUnlock(hGlobal);

		if (!PostMessageW(hWnd, WM_DROPFILES, reinterpret_cast<WPARAM>(hGlobal), NULL))
		{
			GlobalFree(hGlobal);
			return FALSE;
		}

		//std::thread([hWnd, systemOKText]() -> void
		//	{
		//		auto start = std::chrono::high_resolution_clock::now();
		//		while (true)
		//		{
		//			auto hPopup = GetWindow(hWnd, GW_ENABLEDPOPUP);
		//			if (hPopup && hPopup != hWnd && !EnumChildWindows(hPopup, EnumChildProc, reinterpret_cast<LPARAM>(systemOKText)))
		//			{
		//				break;
		//			}

		//			auto end = std::chrono::high_resolution_clock::now();
		//			std::chrono::duration<double, std::milli> elapsed = end - start;
		//			if (elapsed.count() > 400)
		//			{
		//				break;
		//			}
		//		}
		//	}).detach();

		return TRUE;
	}
};

namespace mmdutl
{
	mmp::WinAPIHooker<decltype(_wsopen_s)*> f_wsopen_s;
	mmp::WinAPIHooker<decltype(_write)*> f_write;
	mmp::WinAPIHooker<decltype(_close)*> f_close;

	std::atomic<bool> is_saving;
	std::ofstream save_writer;
	char save_buf[1024 * 1024 * 10];

	bool succeeded;
	HANDLE hSaveFinishedEvent = CreateEventW(nullptr, FALSE, FALSE, L"Global\\MMDLuaSaveProjectFinishedEvent");

	static errno_t Mywsopen_s(int* pfh, const wchar_t* filename, int oflag, int shflag, int pmode)
	{
		printf_s("Opening %S\n", filename);
		succeeded = true;
		std::wstring path = filename;
		auto dot = path.find_last_of(L".");
		auto substr = path.substr(dot);
		auto compare = substr.compare(L".pmm");
		printf_s("oflag=%d; pfh=%p; dot=%zu; substr=%S; compare=%d; is_saving=%d\n", oflag, pfh, dot, substr.c_str(), compare, is_saving.load());
		if (oflag & _O_WRONLY && pfh && dot != std::wstring::npos && compare == 0 && is_saving.exchange(false))
		{
			printf_s("Saving pmm file\n");
			*pfh = -1;

			std::wstring dirPath{};
			if (!mish::GetMMDDirectory(getHWND(), dirPath))
			{
				succeeded = false;
				printf_s("Getting MMD directory failed\n");
				return f_wsopen_s(pfh, filename, oflag, shflag, pmode);
			}

			std::wstring save_path = dirPath + L"\\MMDLua\\project.pmm";

			int file_handle;
			auto result = f_wsopen_s(&file_handle, save_path.c_str(), oflag, shflag, pmode);
			f_close(file_handle);

			std::ofstream ofs(save_path, std::ios::binary);
			ofs.rdbuf()->pubsetbuf(save_buf, sizeof(save_buf));
			if (ofs.is_open() == false)
			{
				succeeded = false;
				printf_s("Opening output file stream failed\n");
				return result;
			}
			save_writer = std::move(ofs);
			return 0;
		}
		return f_wsopen_s(pfh, filename, oflag, shflag, pmode);
	}

	static int __cdecl MyWrite(int fd, const void* buffer, unsigned int count)
	{
		if (fd == -1 && succeeded)
		{
			save_writer.write((const char*)buffer, count);
			return count;
		}
		return f_write(fd, buffer, count);
	}

	static errno_t Myclose(int pfh)
	{
		if (pfh == -1 && succeeded)
		{
			printf_s("Closing pmm file\n");
			std::thread([]()
				{
					save_writer.flush();
					save_writer.close();
					SetEvent(hSaveFinishedEvent);
				}).detach();
			return 0;
		}
		SetEvent(hSaveFinishedEvent);
		return f_close(pfh);
	}
}

namespace mish
{
	static void TraversalMenuItems(HMENU hMenu)
	{
		MENUITEMINFOW mii = {};
		mii.cbSize = sizeof(MENUITEMINFOW);
		mii.fMask = MIIM_TYPE;

		auto count = GetMenuItemCount(hMenu);
		for (int i = 0; i < count; i++)
		{
			GetMenuItemInfoW(hMenu, i, TRUE, &mii);
			mii.dwTypeData = new wchar_t[mii.cch + 1];
			mii.cch++;
			GetMenuItemInfoW(hMenu, i, TRUE, &mii);
			printf_s("HMENU %p [%d]: type=%d; typeData=%S\n", hMenu, i, mii.fType, mii.dwTypeData);
			delete mii.dwTypeData;
			mii.dwTypeData = nullptr;
			mii.cch = 0;
		}
	}

	static void SaveProject(HWND hWnd, UINT saveMenuItemId)
	{
		printf_s("Calling save project method by menu command\n");
		mmdutl::is_saving.store(true);
		SendMessageW(hWnd, WM_COMMAND, MAKEWPARAM(saveMenuItemId, 0), NULL);
	}

	static void SaveProject(HWND hWnd)
	{
		printf_s("Calling save project method by key input\n");
		DWORD currentThreadId = GetCurrentThreadId();
		DWORD targetThreadId = GetWindowThreadProcessId(hWnd, nullptr);
		AttachThreadInput(currentThreadId, targetThreadId, TRUE);

		ShowWindow(hWnd, SW_SHOW);
		SetForegroundWindow(hWnd);
		SetFocus(hWnd);

		INPUT inputs[4] = {};
		ZeroMemory(inputs, sizeof(inputs));

		inputs[0].type = INPUT_KEYBOARD;
		inputs[0].ki.wVk = VK_LCONTROL;

		inputs[1].type = INPUT_KEYBOARD;
		inputs[1].ki.wVk = 'S';

		inputs[2].type = INPUT_KEYBOARD;
		inputs[2].ki.wVk = 'S';
		inputs[2].ki.dwFlags = KEYEVENTF_KEYUP;

		inputs[2].type = INPUT_KEYBOARD;
		inputs[2].ki.wVk = VK_LCONTROL;
		inputs[2].ki.dwFlags = KEYEVENTF_KEYUP;

		mmdutl::is_saving.store(true);
		UINT sent = SendInput(ARRAYSIZE(inputs), inputs, sizeof(INPUT));
		if (sent != ARRAYSIZE(inputs))
		{
			printf_s("SendInput failed: 0x%x\n", HRESULT_FROM_WIN32(GetLastError()));
		}

		AttachThreadInput(currentThreadId, targetThreadId, FALSE);
	}
}
