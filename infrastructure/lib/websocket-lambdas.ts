import { backendCode } from './constants';
import { DynamoTables } from './dynamo-tables';
import { Effect, ManagedPolicy, PolicyStatement, Role, ServicePrincipal } from "aws-cdk-lib/aws-iam";
import { Construct } from "constructs";
import { Function, Runtime } from 'aws-cdk-lib/aws-lambda';
import { Duration, RemovalPolicy } from "aws-cdk-lib";
import { LogGroup, RetentionDays } from "aws-cdk-lib/aws-logs";

export interface WebsocketLambdas {
  connect: Function;
  disconnect: Function;
  default: Function;
  createChannel: Function;
  joinChannel: Function;
  leaveChannel: Function;
  listChannels: Function;
  sendMessage: Function;
  fetchMessages: Function;
  getConnectionInfo: Function;
}

export function createWebsocketLambdas(scope: Construct, tables: DynamoTables): WebsocketLambdas {
  const role = createRole(scope, tables);
  const connect = createFunction(scope,'ConnectFunction', 'AwsServerlessChatroom::AwsServerlessChatroom.Function::OnConnect', role);
  const disconnect = createFunction(scope,'DisconnectFunction', 'AwsServerlessChatroom::AwsServerlessChatroom.Function::OnDisconnect', role);
  const defaultFunc = createFunction(scope,'DefaultFunction', 'AwsServerlessChatroom::AwsServerlessChatroom.Function::Default', role);
  const getConnectionInfo = createFunction(scope,'GetConnectionInfoFunction', 'AwsServerlessChatroom::AwsServerlessChatroom.WebsocketFunctions.GetConnectionInfo::HandleRequest', role);
  const createChannel = createFunction(scope,'CreateChannelFunction', 'AwsServerlessChatroom::AwsServerlessChatroom.WebsocketFunctions.CreateChannel::HandleRequest', role);
  const joinChannel =  createFunction(scope,'JoinChannelFunction', 'AwsServerlessChatroom::AwsServerlessChatroom.WebsocketFunctions.JoinChannel::HandleRequest', role);
  const leaveChannel = createFunction(scope,'LeaveChannelFunction', 'AwsServerlessChatroom::AwsServerlessChatroom.WebsocketFunctions.LeaveChannel::HandleRequest', role);
  const listChannels = createFunction(scope,'ListChannelsFunction', 'AwsServerlessChatroom::AwsServerlessChatroom.WebsocketFunctions.ListChannels::HandleRequest', role);
  const sendMessage = createFunction(scope,'SendMessageFunction', 'AwsServerlessChatroom::AwsServerlessChatroom.WebsocketFunctions.SendMessage::HandleRequest', role);
  const fetchMessages = createFunction(scope,'FetchMessagesFunction', 'AwsServerlessChatroom::AwsServerlessChatroom.WebsocketFunctions.FetchMessages::HandleRequest', role);

  return {
    connect,
    disconnect,
    default: defaultFunc,
    getConnectionInfo,
    createChannel,
    joinChannel,
    leaveChannel,
    listChannels,
    sendMessage,
    fetchMessages,
  };
}

function createRole(scope: Construct, tables: DynamoTables) {
  const role = new Role(scope, 'WebSocketLambdaRole', {
    assumedBy: new ServicePrincipal('lambda.amazonaws.com'),
  });

  role.addManagedPolicy(
    ManagedPolicy.fromAwsManagedPolicyName(
      'service-role/AWSLambdaBasicExecutionRole'
    )
  );

  role.addToPolicy(new PolicyStatement({
    effect: Effect.ALLOW,
    actions: ['execute-api:ManageConnections'],
    resources: ['arn:aws:execute-api:*:*:*/@connections/*']
  }))

  role.addToPolicy(
    new PolicyStatement({
      effect: Effect.ALLOW,
      actions: [
        "dynamodb:BatchGetItem",
        "dynamodb:PutItem",
        "dynamodb:GetItem",
        "dynamodb:UpdateItem",
        "dynamodb:Query",
        "dynamodb:Scan",
        "dynamodb:DeleteItem"
      ],
      resources: [
        tables.joinedChannelTable.tableArn,
        tables.channelSubscriptionsTable.tableArn,
        tables.messagesTable.tableArn,
        tables.channelTable.tableArn,
        tables.messageSequenceTable.tableArn,
      ],
    })
  );

  return role;
}

function createFunction(scope: Construct, id: string, handler: string, role: Role) {
  const func = new Function(scope, id, {
    role,
    runtime: Runtime.DOTNET_6,
    code: backendCode,
    timeout: Duration.seconds(10),
    handler,
  });

  new LogGroup(scope, `LogGroup-${id}`, {
    logGroupName: `/aws/lambda/${func.functionName}`,
    retention: RetentionDays.ONE_WEEK,
    removalPolicy: RemovalPolicy.DESTROY,
  });

  return func;
}