// +------------+
// | Directives |
// +------------+
// Specify Windows version
#define WINVER         0x0600
#define _WIN32_WINNT   0x0600

// Includes
#include <time.h>
#include <d3d9.h>
#include <d3dx9.h>
#include <dwmapi.h>

#include <iostream>
#include <vector>

// Import libraries to link with
#pragma comment(lib, "d3d9.lib")
#pragma comment(lib, "d3dx9.lib")
#pragma comment(lib, "dwmapi.lib")

#define ARGB_TRANS   0x00000000 // 100% alpha
#define MAX_CURSORS 16
#define SPRITE_SIZE 128

#define ANIMATION_DURATION 100

#define TEXTURE_PATH L"Resources\\circle.png"


struct D3DCURSOR
{
	FLOAT		x,y,rotation,last_rendered_x,last_rendered_y,scaling,snapshot_scaling;
	BOOL		hidden,enabled,pressed;
	DWORD		color;
	clock_t		animationStart;
};

struct CURSORPTR
{
	D3DCURSOR *cursor;
};

// +---------+
// | Globals |
// +---------+
WCHAR                   *g_wcpAppName  = L"D3DCursor";
INT                     g_iWidth       = 800;
INT                     g_iHeight      = 600;
MARGINS                 g_mgDWMMargins = {-1, -1, -1, -1};
IDirect3D9Ex            *g_pD3D        = NULL;
IDirect3DDevice9Ex      *g_pD3DDevice  = NULL;
IDirect3DVertexBuffer9  *g_pVB         = NULL;
D3DCURSOR				*cursors = new D3DCURSOR[MAX_CURSORS];
LPD3DXSPRITE g_sprite=NULL;
LPDIRECT3DTEXTURE9 g_circle=NULL;
INT enabledCursors = 0;

FLOAT screenRelativeCursorScale = 0.02f;
FLOAT normalCursorScale = 0.5f;
FLOAT pressedCursorScale = 0.4f;
FLOAT hiddenCursorScale = 0.0f;

HWND       hWnd  = NULL;

D3DXMATRIX Identity;

CURSORPTR				*clearQueue = new CURSORPTR[MAX_CURSORS];
INT nToClear=0;

BOOL wait = true;


// +--------------+
// | D3DStartup() |
// +--------------+----------------------------------+
// | Initialise Direct3D and perform once only tasks |
// +-------------------------------------------------+
HRESULT D3DStartup(HWND hWnd)
{
	BOOL                  bCompOk             = FALSE;   // Is composition enabled? 
	D3DPRESENT_PARAMETERS pp;                            // Presentation prefs
	DWORD                 msqAAQuality        = 0;       // Non-maskable quality

  
  
	D3DXMATRIX Ortho2D;    

	// Make sure that DWM composition is enabled
	DwmIsCompositionEnabled(&bCompOk);
	if(!bCompOk) return E_FAIL;

	// Create a Direct3D object
	if(FAILED(Direct3DCreate9Ex(D3D_SDK_VERSION, &g_pD3D))) return E_FAIL;

	// Setup presentation parameters
	ZeroMemory(&pp, sizeof(pp));
	pp.Windowed            = TRUE;
	pp.SwapEffect          = D3DSWAPEFFECT_DISCARD; 
	pp.BackBufferFormat    = D3DFMT_A8R8G8B8;       // Back buffer format with alpha channel
	pp.PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE; //Disables vsync

	pp.MultiSampleType = D3DMULTISAMPLE_NONE;

	// Create a Direct3D device object
	if(FAILED(g_pD3D->CreateDeviceEx(D3DADAPTER_DEFAULT,
									D3DDEVTYPE_HAL,
									hWnd,
									D3DCREATE_HARDWARE_VERTEXPROCESSING,
									&pp,
									NULL,
									&g_pD3DDevice
									))) return E_FAIL;

	// Configure the device state
  
	g_pD3DDevice->SetRenderState(D3DRS_LIGHTING, FALSE);

	

		D3DXMatrixOrthoLH(&Ortho2D, g_iWidth, g_iHeight, 0.0f, 1.0f);
	D3DXMatrixIdentity(&Identity);

	g_pD3DDevice->SetTransform(D3DTS_PROJECTION, &Ortho2D);
	g_pD3DDevice->SetTransform(D3DTS_WORLD, &Identity);
	g_pD3DDevice->SetTransform(D3DTS_VIEW, &Identity);


	g_pD3DDevice->SetRenderState( D3DRS_ALPHABLENDENABLE, TRUE);

	return S_OK;
}

// +---------------+
// | D3DShutdown() |
// +---------------+----------------------+
// | Release all created Direct3D objects |
// +--------------------------------------+
VOID D3DShutdown(VOID)
{
  if(g_pVB != NULL ) g_pVB->Release();
  if(g_pD3DDevice != NULL) g_pD3DDevice->Release();
  if(g_pD3D != NULL) g_pD3D->Release();
}

// +--------------+
// | CreateCube() |
// +--------------+------------------------------+
// | Populates a vertex buffer with a cube shape |
// +---------------------------------------------+
HRESULT InitSprites(VOID)
{
	if (SUCCEEDED(D3DXCreateSprite(g_pD3DDevice,&g_sprite)))
	{
		// created OK
	}

	D3DXCreateTextureFromFile(g_pD3DDevice,TEXTURE_PATH, &g_circle );

	return S_OK;
}
FLOAT easeInOutQuint(FLOAT elapsedTime, FLOAT startValue, FLOAT changeInValue, FLOAT duration) {
	elapsedTime /= duration/2;
	if (elapsedTime < 1) return changeInValue/2*elapsedTime*elapsedTime*elapsedTime*elapsedTime*elapsedTime + startValue;
	elapsedTime -= 2;
	return changeInValue/2*(elapsedTime*elapsedTime*elapsedTime*elapsedTime*elapsedTime + 2) + startValue;
};
// +----------+
// | Render() |
// +----------+-------------------------+
// | Renders a scene to the back buffer |
// +------------------------------------+
VOID Render(VOID)
{
	if (!wait)
	{
		std::vector<RECT> dirtyRects;

		D3DXMATRIX    scaleMatrix;
		D3DXMATRIX	positionMatrix;
		// Sanity check
		if (g_pD3DDevice == NULL) return;
		if (g_sprite == NULL) return;

		D3DRECT *clearRect = new D3DRECT[enabledCursors + nToClear];
		int j = -1;
		int i = 0;
		for (i = 0; i < enabledCursors; i++)
		{
			while (!cursors[++j].enabled)
			{

			}

			clearRect[i].x1 = cursors[j].last_rendered_x - (SPRITE_SIZE * normalCursorScale);
			clearRect[i].x2 = cursors[j].last_rendered_x + (SPRITE_SIZE * normalCursorScale);
			clearRect[i].y1 = cursors[j].last_rendered_y - (SPRITE_SIZE * normalCursorScale);
			clearRect[i].y2 = cursors[j].last_rendered_y + (SPRITE_SIZE * normalCursorScale);

			RECT dirtyRect;
			dirtyRect.left = clearRect[i].x1;
			dirtyRect.right = clearRect[i].x2;
			dirtyRect.top = clearRect[i].y1;
			dirtyRect.bottom = clearRect[i].y2;

			dirtyRects.push_back(dirtyRect);
		}
		for (j = 0; i < enabledCursors + nToClear; i++)
		{
			clearRect[i].x1 = clearQueue[j].cursor->last_rendered_x - (SPRITE_SIZE * normalCursorScale);
			clearRect[i].x2 = clearQueue[j].cursor->last_rendered_x + (SPRITE_SIZE * normalCursorScale);
			clearRect[i].y1 = clearQueue[j].cursor->last_rendered_y - (SPRITE_SIZE * normalCursorScale);
			clearRect[i].y2 = clearQueue[j].cursor->last_rendered_y + (SPRITE_SIZE * normalCursorScale);

			RECT dirtyRect;
			dirtyRect.left = clearRect[i].x1;
			dirtyRect.right = clearRect[i].x2;
			dirtyRect.top = clearRect[i].y1;
			dirtyRect.bottom = clearRect[i].y2;

			dirtyRects.push_back(dirtyRect);

			j++;

		}


		g_pD3DDevice->Clear(enabledCursors + nToClear, clearRect, D3DCLEAR_TARGET, ARGB_TRANS, 1.0f, 0);

		nToClear = 0;
		// Render scene
		if (SUCCEEDED(g_pD3DDevice->BeginScene()))
		{
			D3DXVECTOR2 pos;
			RECT size;
			D3DXVECTOR2 spriteCentre = D3DXVECTOR2(64.0f, 64.0f);
			D3DXMATRIX mat;
			D3DXVECTOR2 scaling;

			size.top = 0;
			size.left = 0;
			size.right = SPRITE_SIZE;
			size.bottom = SPRITE_SIZE;

			if (SUCCEEDED(g_sprite->Begin(D3DXSPRITE_ALPHABLEND)))
			{
				j = -1;
				for (int i = 0; i < enabledCursors; i++)
				{
					while (!cursors[++j].enabled)
					{

					}
					cursors[j].last_rendered_x = cursors[j].x;
					cursors[j].last_rendered_y = cursors[j].y;

					pos.x = cursors[j].x - (SPRITE_SIZE / 2);
					pos.y = cursors[j].y - (SPRITE_SIZE / 2);

					RECT dirtyRect;
					dirtyRect.left = pos.x;
					dirtyRect.right = pos.x + SPRITE_SIZE;
					dirtyRect.top = pos.y;
					dirtyRect.bottom = pos.y + SPRITE_SIZE;

					dirtyRects.push_back(dirtyRect);

					//Animation
					if (cursors[j].hidden)
					{
						if (cursors[j].scaling == hiddenCursorScale)
						{

						}
						else if (abs(cursors[j].scaling - hiddenCursorScale) > 0.01)
						{
							float diff = (((float)clock() - (float)cursors[j].animationStart) / CLOCKS_PER_SEC) * 1000;
							cursors[j].scaling = easeInOutQuint(diff, cursors[j].snapshot_scaling, hiddenCursorScale - cursors[j].snapshot_scaling, ANIMATION_DURATION);
						}
						else
						{
							cursors[j].scaling = hiddenCursorScale;
						}
					}
					else if (cursors[j].pressed)
					{
						if (cursors[j].scaling == pressedCursorScale)
						{

						}
						else if (abs(cursors[j].scaling - pressedCursorScale) > 0.01)
						{
							float diff = (((float)clock() - (float)cursors[j].animationStart) / CLOCKS_PER_SEC) * 1000;
							cursors[j].scaling = easeInOutQuint(diff, cursors[j].snapshot_scaling, pressedCursorScale - cursors[j].snapshot_scaling, ANIMATION_DURATION);
						}
						else
						{
							cursors[j].scaling = pressedCursorScale;
						}
					}
					else if (!cursors[j].pressed)
					{
						if (cursors[j].scaling == normalCursorScale)
						{

						}
						else if (abs(cursors[j].scaling - normalCursorScale) > 0.01)
						{
							float diff = (((float)clock() - (float)cursors[j].animationStart) / CLOCKS_PER_SEC) * 1000;
							cursors[j].scaling = easeInOutQuint(diff, cursors[j].snapshot_scaling, normalCursorScale - cursors[j].snapshot_scaling, ANIMATION_DURATION);
						}
						else
						{
							cursors[j].scaling = normalCursorScale;
						}
					}

					if (cursors[j].scaling < 0)
					{
						cursors[j].scaling = hiddenCursorScale;
					}

					if (cursors[j].scaling > normalCursorScale)
					{
						cursors[j].scaling = normalCursorScale;
					}

					scaling.x = cursors[j].scaling;
					scaling.y = cursors[j].scaling;
					D3DXMatrixTransformation2D(&mat, &spriteCentre, 0.0, &scaling, &spriteCentre, 0, &pos);
					g_sprite->SetTransform(&mat);
					g_sprite->Draw(g_circle, NULL, NULL, NULL, 0xff000000 | cursors[j].color);

					scaling.x *= 0.9f;
					scaling.y *= 0.9f;
					D3DXMatrixTransformation2D(&mat, &spriteCentre, 0.0, &scaling, &spriteCentre, 0, &pos);
					g_sprite->SetTransform(&mat);
					g_sprite->Draw(g_circle, NULL, NULL, NULL, 0xff000000);

					scaling.x *= 0.5f;
					scaling.y *= 0.5f;
					D3DXMatrixTransformation2D(&mat, &spriteCentre, 0.0, &scaling, &spriteCentre, 0, &pos);
					g_sprite->SetTransform(&mat);
					g_sprite->Draw(g_circle, NULL, NULL, NULL, 0xffFFFFFF);

				}

				g_sprite->End();
			}

			g_pD3DDevice->EndScene();
		}
		RECT dummy;
		dummy.left = 0;
		dummy.right = 0;
		dummy.bottom = 0;
		dummy.top = 0;
		dirtyRects.push_back(dummy);

		DWORD size = dirtyRects.size() * sizeof(RECT)+sizeof(RGNDATAHEADER);

		RGNDATA *rgndata = NULL;

		//allocate  the memory
		rgndata = (RGNDATA *)HeapAlloc(GetProcessHeap(), 0, size);

		if (!rgndata)
			return;

		RECT* pRectInitial = (RECT*)rgndata->Buffer;
		RECT rectBounding = dirtyRects[0];

		//feeding the rectangles into the buffer of rgndata
		for (int i = 0; i < dirtyRects.size(); i++)
		{
			RECT rectCurrent = dirtyRects[i];
			rectBounding.left = min(rectBounding.left, rectCurrent.left);
			rectBounding.right = max(rectBounding.right, rectCurrent.right);
			rectBounding.top = min(rectBounding.top, rectCurrent.top);
			rectBounding.bottom = max(rectBounding.bottom, rectCurrent.bottom);

			*pRectInitial = dirtyRects[i];
			pRectInitial++;
		}

		//preparing rgndata header
		RGNDATAHEADER  header;
		header.dwSize = sizeof(RGNDATAHEADER);
		header.iType = RDH_RECTANGLES;
		header.nCount = dirtyRects.size();
		header.nRgnSize = dirtyRects.size() * sizeof(RECT);
		header.rcBound.left = rectBounding.left;
		header.rcBound.top = rectBounding.top;
		header.rcBound.right = rectBounding.right;
		header.rcBound.bottom = rectBounding.bottom;

		rgndata->rdh = header;

		// Update display
		g_pD3DDevice->PresentEx(NULL, NULL, NULL, rgndata, 0);
	}
}

// +--------------+
// | WindowProc() |
// +--------------+------------------+
// | The main window message handler |
// +---------------------------------+
LRESULT WINAPI WindowProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
  switch(uMsg)
  {
    case WM_DESTROY:
      // Signal application to terminate
      PostQuitMessage(0);
      return 0;

    case WM_ERASEBKGND:
      // We dont want to call render twice so just force Render() in WM_PAINT to be called
      //SendMessage(hWnd, WM_PAINT, NULL, NULL);
      return TRUE;

    /*case WM_PAINT:
      // Force a render to keep the window updated
	  Sleep(10);
      return 0;*/
  }

  return DefWindowProc(hWnd, uMsg, wParam, lParam);
}

VOID recalculateCursorScale(FLOAT scale)
{
	normalCursorScale = (scale * g_iWidth) / SPRITE_SIZE;
	//normalCursorScale = normalCursorScale > 1.0f ? 1.0f : normalCursorScale;
	pressedCursorScale = 0.8f*normalCursorScale;
}

// +-----------+
// | WinMain() |
// +-----------+---------+
// | Program entry point |
// +---------------------+
extern "C" __declspec(dllexport)INT WINAPI StartD3DCursorWindow(HINSTANCE hInstance, HWND hParent, int x, int y, int width, int height, bool topmost, float cursorScale)
{
  MSG        uMsg;     
  WNDCLASSEX wc    = {sizeof(WNDCLASSEX),              // cbSize
                      NULL,                            // style
                      WindowProc,                      // lpfnWndProc
                      NULL,                            // cbClsExtra
                      NULL,                            // cbWndExtra
                      hInstance,                       // hInstance
                      LoadIcon(NULL, IDI_APPLICATION), // hIcon
                      LoadCursor(NULL, IDC_ARROW),     // hCursor
                      NULL,                            // hbrBackground
                      NULL,                            // lpszMenuName
                      g_wcpAppName,                    // lpszClassName
                      LoadIcon(NULL, IDI_APPLICATION)};// hIconSm

  RegisterClassEx(&wc);

  g_iWidth = width;
  g_iHeight = height;

  screenRelativeCursorScale = cursorScale;

  recalculateCursorScale(screenRelativeCursorScale);

  hWnd = CreateWindowEx(WS_EX_COMPOSITED | WS_EX_LAYERED | WS_EX_TRANSPARENT,             // dwExStyle
                        g_wcpAppName,                 // lpClassName
                        g_wcpAppName,                 // lpWindowName
						WS_POPUP,        // dwStyle
                        x, y, // x, y
                        g_iWidth, g_iHeight,          // nWidth, nHeight
                        hParent,                         // hWndParent
                        NULL,                         // hMenu
                        hInstance,                    // hInstance
                        NULL);                        // lpParam

  // Extend glass to cover whole window
  DwmExtendFrameIntoClientArea(hWnd, &g_mgDWMMargins);

  SetLayeredWindowAttributes(hWnd, 0, 180, LWA_ALPHA);
 
  HWND zpos = topmost ? HWND_TOPMOST : HWND_NOTOPMOST;

  SetWindowPos(hWnd,zpos,0,0,0,0,SWP_NOMOVE|SWP_NOSIZE|SWP_NOACTIVATE);

  // Initialise Direct3D
  if(SUCCEEDED(D3DStartup(hWnd)))
  {
    if(SUCCEEDED(InitSprites()))
    {
      // Show the window
      ShowWindow(hWnd, SW_SHOWDEFAULT);
      UpdateWindow(hWnd);
    }
  }

  wait = false;
  // Shutdown Direct3D
  //D3DShutdown();

  // Exit application
  return 0;
}

extern "C" __declspec(dllexport)VOID WINAPI SetCursorScale(float cursorScale)
{
	screenRelativeCursorScale = cursorScale;
	recalculateCursorScale(screenRelativeCursorScale);
}

extern "C" __declspec(dllexport)VOID WINAPI SetD3DCursorWindowPosition(int x, int y, int width, int height, bool topmost)
{
	g_iWidth = width;
	g_iHeight = height;
	HWND zpos = topmost ? HWND_TOPMOST : HWND_NOTOPMOST;
	wait = true;
	SetWindowPos(hWnd, zpos, x, y, g_iWidth, g_iHeight, SWP_NOACTIVATE);
	D3DShutdown();

	recalculateCursorScale(screenRelativeCursorScale);

	if (SUCCEEDED(D3DStartup(hWnd)))
	{
		if (SUCCEEDED(InitSprites()))
		{
			// Show the window
			ShowWindow(hWnd, SW_SHOWDEFAULT);
			UpdateWindow(hWnd);
		}
	}
	wait = false;
	//D3DXMATRIX Ortho2D;
	//D3DXMatrixOrthoLH(&Ortho2D, g_iWidth, g_iHeight, 0.0f, 1.0f);
	//g_pD3DDevice->SetTransform(D3DTS_PROJECTION, &Ortho2D);
}

extern "C" __declspec(dllexport)VOID WINAPI RenderAllD3DCursors()
{
	if (!wait)
	{
		try {
			Render();
		}
		catch (...) {}
	}
}

extern "C" __declspec(dllexport)VOID WINAPI SetD3DCursorPosition(int id, int x, int y)
{
	cursors[id].x = x;
	cursors[id].y = y;
}

extern "C" __declspec(dllexport)VOID WINAPI SetD3DCursorPressed(int id, bool pressed)
{
	if(cursors[id].pressed != pressed)
	{
		cursors[id].pressed = pressed;
		cursors[id].animationStart = clock();
		cursors[id].snapshot_scaling = cursors[id].scaling;
	}
}

extern "C" __declspec(dllexport)VOID WINAPI SetD3DCursorHidden(int id, bool hidden)
{
	if(cursors[id].hidden != hidden)
	{
		cursors[id].hidden = hidden;
		cursors[id].animationStart = clock();
		cursors[id].snapshot_scaling = cursors[id].scaling;
	}
}

extern "C" __declspec(dllexport)VOID WINAPI AddD3DCursor(int id, DWORD color)
{
	if(id >= MAX_CURSORS)
	{

	}
	else
	{
		D3DCURSOR newcursor;

		newcursor.x = 0;
		newcursor.y = 0;
		newcursor.rotation = 0;
		newcursor.last_rendered_x = 0;
		newcursor.last_rendered_y = 0;
		newcursor.scaling = normalCursorScale;
		newcursor.hidden = false;
		newcursor.enabled = true;
		newcursor.color = color;

		cursors[id] = newcursor;

		enabledCursors++;
	}
}

extern "C" __declspec(dllexport)VOID WINAPI RemoveD3DCursor(int id)
{
	cursors[id].enabled = false;
	enabledCursors--;
	clearQueue[nToClear].cursor = &cursors[id];
	nToClear++;
}