import { RemovalPolicy, Stack, StackProps } from 'aws-cdk-lib';
import { Construct } from 'constructs';
import { Function, Runtime, Code } from 'aws-cdk-lib/aws-lambda';
import { WebSocketApi, WebSocketStage } from '@aws-cdk/aws-apigatewayv2-alpha';
import { WebSocketLambdaIntegration } from '@aws-cdk/aws-apigatewayv2-integrations-alpha';
import { Table, AttributeType } from 'aws-cdk-lib/aws-dynamodb';
import { Effect, ManagedPolicy, PolicyStatement, Role, ServicePrincipal } from 'aws-cdk-lib/aws-iam';
import { readFileSync } from 'fs';

export class InfrastructureStack extends Stack {
  constructor(scope: Construct, id: string, props?: StackProps) {
    super(scope, id, props);

    const activityLog = this.createActivityLogTable();

    const connectionHandler = this.createConnectionHandler(activityLog);
    const wsApi = this.createWebSocketApi(connectionHandler);
    const wsStage = this.createWsStage(wsApi);
  }

  private createWsStage(wsApi: WebSocketApi) {
    return new WebSocketStage(this, 'WebSocketStage', {
      webSocketApi: wsApi,
      stageName: 'prod',
      autoDeploy: true,
    });
  }

  private createActivityLogTable() {
    return new Table(this, 'ActivityLogTable', {
      tableName: 'ServerlessChatroomApi-ActivityLog',
      partitionKey: { name: 'SessionId', type: AttributeType.STRING },
      sortKey: { name: 'Timestamp', type: AttributeType.NUMBER },
      removalPolicy: RemovalPolicy.DESTROY,
    });
  }

  private createWebSocketApi(connectionHandler: Function): WebSocketApi {
    return new WebSocketApi(this, 'ServerlessChatroomApi', {
      connectRouteOptions: { integration: new WebSocketLambdaIntegration('ConnectIntegration', connectionHandler) },
      disconnectRouteOptions: { integration: new WebSocketLambdaIntegration('DisconnectIntegration',connectionHandler) },
      defaultRouteOptions: { integration: new WebSocketLambdaIntegration('DefaultIntegration', connectionHandler) },
    });
  }

  private createConnectionHandler(table: Table): Function {
    const role = this.createLambdaRole(table);

    const code = readFileSync('./lib/connection-handler-code.txt').toString();
    return new Function(this, 'HelloWorld', {
      role,
      runtime: Runtime.NODEJS_16_X,
      handler: 'index.handler',
      code: Code.fromInline(code),
    });
  }

  private createLambdaRole(table: Table) {
    const role = new Role(this, 'HelloWorldFunctionRole', {
      assumedBy: new ServicePrincipal('lambda.amazonaws.com'),
    });

    role.addManagedPolicy(
      ManagedPolicy.fromAwsManagedPolicyName(
        'service-role/AWSLambdaBasicExecutionRole'
      )
    );

    role.addToPolicy(
      new PolicyStatement({
        effect: Effect.ALLOW,
        actions: [
          "dynamodb:BatchGetItem",
          "dynamodb:PutItem",
          "dynamodb:GetItem",
          "dynamodb:UpdateItem"
        ],
        resources: [table.tableArn],
      })
    );
    return role;
  }
}
