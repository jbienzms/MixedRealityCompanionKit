/* -LICENSE-START-
** Copyright (c) 2013 Blackmagic Design
**
** Permission is hereby granted, free of charge, to any person or organization
** obtaining a copy of the software and accompanying documentation covered by
** this license (the "Software") to use, reproduce, display, distribute,
** execute, and transmit the Software, and to prepare derivative works of the
** Software, and to permit third-parties to whom the Software is furnished to
** do so, all subject to the following:
**
** The copyright notices in the Software and this entire statement, including
** the above license grant, this restriction and the following disclaimer,
** must be included in all copies of the Software, in whole or in part, and
** all derivative works of the Software, unless such copies or derivative
** works are solely in the form of machine-executable object code generated by
** a source language processor.
**
** THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
** IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
** FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
** SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
** FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
** ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
** DEALINGS IN THE SOFTWARE.
** -LICENSE-END-
*/

#pragma once

#if USE_DECKLINK || USE_DECKLINK_SHUTTLE

#include <Windows.h>
#include <vector>
#include "DeckLinkAPI_h.h"
#include "DirectXHelper.h"
#include <string>
#include "BufferedTextureFetch.h"

class DeckLinkDevice : public IDeckLinkInputCallback
{
private:
    enum PixelFormat
    {
        YUV,
        BGRA
    };

    PixelFormat pixelFormat = PixelFormat::YUV;

    ULONG                     m_refCount;
    IDeckLink*                m_deckLink;
    IDeckLinkInput*           m_deckLinkInput;
    IDeckLinkOutput*          m_deckLinkOutput;
    BMDTimeScale              m_playbackTimeScale;
    BOOL                      m_supportsFormatDetection;
    bool                      m_currentlyCapturing;
    CRITICAL_SECTION          m_captureCardCriticalSection;
    CRITICAL_SECTION          m_frameAccessCriticalSection;
    CRITICAL_SECTION          m_outputCriticalSection;

    BYTE* localFrameBuffer;
    BYTE* rawBuffer = new BYTE[FRAME_BUFSIZE_RAW];

    BYTE* latestBuffer = new BYTE[FRAME_BUFSIZE];
    BYTE* outputBuffer = new BYTE[FRAME_BUFSIZE];

    IDeckLinkMutableVideoFrame* outputFrame = NULL;

    BMDTimeValue frameDuration = 0;

    class BufferCache
    {
    public:
        BYTE * buffer;
        LONGLONG timeStamp;
    };

    BufferCache bufferCache[MAX_NUM_CACHED_BUFFERS];
    int captureFrameIndex;

    bool dirtyFrame = true;

    ID3D11ShaderResourceView* _colorSRV = nullptr;
    ID3D11Texture2D* _outputTexture = nullptr;
    ID3D11Device* device = nullptr;

    BufferedTextureFetch outputTextureBuffer;

public:
    DeckLinkDevice(IDeckLink* device);
    virtual ~DeckLinkDevice();

    bool                                Init(ID3D11ShaderResourceView* colorSRV);
    bool                                IsCapturing() { return m_currentlyCapturing; };
    bool                                SupportsFormatDetection() { return (m_supportsFormatDetection == TRUE); };
    bool                                StartCapture(BMDDisplayMode videoDisplayMode);
    void                                StopCapture();
    IDeckLink*                          DeckLinkInstance() { return m_deckLink; }

    void Update(int compositeFrameIndex);

    bool supportsOutput = true;

    // IUnknown interface
    virtual HRESULT  STDMETHODCALLTYPE    QueryInterface(REFIID iid, LPVOID *ppv);
    virtual ULONG    STDMETHODCALLTYPE    AddRef();
    virtual ULONG    STDMETHODCALLTYPE    Release();

    // IDeckLinkInputCallback interface
    virtual HRESULT  STDMETHODCALLTYPE    VideoInputFormatChanged(/* in */ BMDVideoInputFormatChangedEvents notificationEvents, /* in */ IDeckLinkDisplayMode *newDisplayMode, /* in */ BMDDetectedVideoInputFormatFlags detectedSignalFlags);
    virtual HRESULT  STDMETHODCALLTYPE    VideoInputFrameArrived(/* in */ IDeckLinkVideoInputFrame* frame, /* in */ IDeckLinkAudioInputPacket* audioPacket);

    LONGLONG GetTimestamp(int frame)
    {
        return bufferCache[frame % MAX_NUM_CACHED_BUFFERS].timeStamp;
    }

    LONGLONG GetDurationHNS()
    {
        return frameDuration;
    }

    int GetCaptureFrameIndex()
    {
        return captureFrameIndex;
    }

    bool OutputYUV();

    void SetOutputTexture(ID3D11Texture2D* outputTexture)
    {
        _outputTexture = outputTexture;
    }
};

class DeckLinkDeviceDiscovery : public IDeckLinkDeviceNotificationCallback
{
private:
    IDeckLinkDiscovery * m_deckLinkDiscovery;
    ULONG                               m_refCount;
    IDeckLink*                          m_deckLink = nullptr;

public:
    DeckLinkDeviceDiscovery();
    virtual ~DeckLinkDeviceDiscovery();

    IDeckLink*                          GetDeckLink() { return m_deckLink; }

    bool                                Enable();
    void                                Disable();

    // IDeckLinkDeviceNotificationCallback interface
    virtual HRESULT  STDMETHODCALLTYPE    DeckLinkDeviceArrived(/* in */ IDeckLink* deckLink);
    virtual HRESULT  STDMETHODCALLTYPE    DeckLinkDeviceRemoved(/* in */ IDeckLink* deckLink);

    virtual HRESULT  STDMETHODCALLTYPE    QueryInterface(REFIID iid, LPVOID *ppv);
    virtual ULONG    STDMETHODCALLTYPE    AddRef();
    virtual ULONG    STDMETHODCALLTYPE    Release();
};

#endif
