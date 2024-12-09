using System.Runtime.Serialization;

namespace Sefirah.App.Data.Enums;
public enum SocketMessageType
{
    [EnumMember(Value = "0")]
    Response,
    [EnumMember(Value = "1")]
    Clipboard,
    [EnumMember(Value = "2")]
    Notification,
    [EnumMember(Value = "3")]
    DeviceInfo,
    [EnumMember(Value = "4")]
    DeviceStatus,
    [EnumMember(Value = "5")]
    PlaybackData,
    [EnumMember(Value = "6")]
    CommandType,
    [EnumMember(Value = "7")]
    FileTransferType,
    [EnumMember(Value = "8")]
    StorageInfo,
    [EnumMember(Value = "9")]
    DirectoryInfo,
    [EnumMember(Value = "10")]
    ScreenData,
    [EnumMember(Value = "11")]
    InteractiveControlMessage,
    [EnumMember(Value = "12")]
    ApplicationInfo,
    [EnumMember(Value = "13")]
    NotificationAction,
    [EnumMember(Value = "14")]
    ReplyAction,
    [EnumMember(Value = "15")]
    SftpServerInfo
}

public enum InteractiveControlType
{
    [EnumMember(Value = "SINGLE")]
    SingleTapEvent,
    [EnumMember(Value = "HOLD")]
    HoldTapEvent,
    [EnumMember(Value = "SWIPE")]
    SwipeEvent,
    [EnumMember(Value = "KEYBOARD")]
    KeyboardEvent,
    [EnumMember(Value = "SCROLL")]
    ScrollEvent,
    [EnumMember(Value = "KEY")]
    KeyEvent,
}

public enum MediaAction
{
    RESUME,
    PAUSE,
    NEXT_QUEUE,
    PREV_QUEUE,
    SEEK,
    VOLUME
}

public enum CommandType
{
    LOCK,
    SHUTDOWN,
    SLEEP,
    HIBERNATE,
    MIRROR,
    CLOSE_MIRROR,
    CLEAR_NOTIFICATIONS
}

public enum NotificationType
{
    ACTIVE,
    REMOVED,
    NEW,
    ACTION
}

public enum FileTransferType
{
    HTTP,
    WEBSOCKET,
    TCP
}
public enum DataTransferType
{
    CHUNK,
    METADATA,
}

public enum ScrollDirection
{
    UP, DOWN
}

public enum KeyboardActionType
{
    Tab, Backspace, Enter, Escape, CtrlC, CtrlV, CtrlX, CtrlA, CtrlZ, CtrlY, Shift
}