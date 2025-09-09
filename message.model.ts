export interface Message {
  id: number;
  senderId: number;
  senderName: string;
  receiverId: number;
  receiverName: string;
  content: string;
  timestamp: string;
  isRead: boolean;
  propertyId?: number;
  propertyTitle?: string;
}

export interface SendMessageRequest {
  receiverId: number;
  content: string;
  propertyId?: number;
}

export interface Conversation {
  otherUserId: number;
  otherUserName: string;
  otherUserRole?: string;
  messages: Message[];
  unreadCount: number;
  lastMessageTime: string;
}

