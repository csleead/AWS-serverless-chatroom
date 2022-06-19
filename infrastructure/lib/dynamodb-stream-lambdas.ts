import { backendCode } from './constants';
import { Construct } from "constructs";
import { Function, Runtime, StartingPosition } from 'aws-cdk-lib/aws-lambda';
import { DynamoTables } from './dynamo-tables';
import { Effect, ManagedPolicy, PolicyStatement, Role, ServicePrincipal } from 'aws-cdk-lib/aws-iam';
import { Duration, RemovalPolicy } from 'aws-cdk-lib';
import { LogGroup, RetentionDays } from 'aws-cdk-lib/aws-logs';

export function createDynamoDbStreamLambda(scope: Construct, tables: DynamoTables): Function {
  const role = new Role(scope, 'OnNewMessagesLambdaRole', {
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
  }));

  tables.channelSubscriptionsTable.grantReadData(role);
  tables.messagesTable.grantStreamRead(role);

  const lambda = new Function(scope, 'OnNewMessages', {
    role,
    runtime: Runtime.DOTNET_6,
    handler: 'AwsServerlessChatroom::AwsServerlessChatroom.Function::OnNewMessages',
    code: backendCode,
    timeout: Duration.seconds(10),
  });

  lambda.addEventSourceMapping('MessagesTableStream', {
    eventSourceArn: tables.messagesTable.tableStreamArn,
    startingPosition: StartingPosition.TRIM_HORIZON,
    retryAttempts: 3,
  });

  new LogGroup(scope, `LogGroup-OnNewMessages`, {
    logGroupName: `/aws/lambda/${lambda.functionName}`,
    retention: RetentionDays.ONE_WEEK,
    removalPolicy: RemovalPolicy.DESTROY,
  });

  return lambda;
}