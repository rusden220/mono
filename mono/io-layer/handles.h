#ifndef _WAPI_HANDLES_H_
#define _WAPI_HANDLES_H_

#define INVALID_HANDLE_VALUE (WapiHandle *)-1

typedef struct _WapiHandle WapiHandle;

extern gboolean CloseHandle(WapiHandle *handle);
extern guint32 SignalObjectAndWait(WapiHandle *signal_handle, WapiHandle *wait,
				   guint32 timeout, gboolean alertable);

#endif /* _WAPI_HANDLES_H_ */
