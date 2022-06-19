import { RemovalPolicy } from "aws-cdk-lib";
import { AttributeType, StreamViewType, Table } from "aws-cdk-lib/aws-dynamodb";
import { Construct } from "constructs";

export interface DynamoTables {
  joinedChannelTable: Table;
  channelSubscriptionsTable: Table;
  messagesTable: Table;
  channelTable: Table;
}

export function createDynamoTables(scope: Construct): DynamoTables {
  const channelSubscriptionsTable = new Table(scope, 'ChannelSubscriptions', {
    tableName: 'ServerlessChatroomApi-ChannelSubscriptions',
    partitionKey: { name: 'ChannelId', type: AttributeType.STRING },
    sortKey: { name: 'ConnectionId', type: AttributeType.STRING },
    removalPolicy: RemovalPolicy.DESTROY,
  });

  const joinedChannelTable = new Table(scope, 'ConnectionLogsTable', {
    tableName: 'ServerlessChatroomApi-ConnectionLogs',
    partitionKey: { name: 'ConnectionId', type: AttributeType.STRING },
    sortKey: { name: 'Timestamp', type: AttributeType.NUMBER },
    removalPolicy: RemovalPolicy.DESTROY,
  });

  const channelTable = new Table(scope, 'ChannelsTable', {
    tableName: 'ServerlessChatroomApi-Channels',
    partitionKey: { name: 'ChannelId', type: AttributeType.STRING },
    removalPolicy: RemovalPolicy.DESTROY,
  });

  const messagesTable = new Table(scope, 'MessagesTable', {
    tableName: 'ServerlessChatroomApi-Messages',
    partitionKey: { name: 'ChannelId', type: AttributeType.STRING },
    sortKey: { name: 'Timestamp', type: AttributeType.NUMBER },
    removalPolicy: RemovalPolicy.DESTROY,
    stream: StreamViewType.NEW_IMAGE,
  });

  return {
    joinedChannelTable,
    channelSubscriptionsTable,
    messagesTable,
    channelTable,
  };
}
