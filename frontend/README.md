# Chat Frontend Demo

React app đơn giản để demo các tính năng chat realtime.

## Cài đặt

```bash
npm install
```

## Chạy

```bash
npm run dev
```

App sẽ chạy ở `http://localhost:3000`

## Tính năng

- ✅ Đăng nhập
- ✅ Danh sách cuộc trò chuyện
- ✅ Chat realtime với SignalR
- ✅ Typing indicator
- ✅ Chia sẻ Date Plan trong chat
- ✅ Group chat support
- ✅ Unread message count

## Cấu hình

Backend API phải chạy ở `http://localhost:5000`

Nếu backend chạy ở port khác, sửa trong file `vite.config.js`:

```js
server: {
  proxy: {
    '/api': {
      target: 'http://localhost:YOUR_PORT',
      ...
    }
  }
}
```

## Sử dụng

1. Đăng nhập bằng email/password có sẵn trong hệ thống
2. Chọn cuộc trò chuyện từ sidebar
3. Gửi tin nhắn
4. Nhấn "Chia sẻ Date Plan" để share date plan vào chat
