﻿// Copyright (c) Yan Cui 2012

// Email : theburningmonk@gmail.com
// Blog  : http://theburningmonk.com

module DynamoDb.SQL.Execution.Helper.Tests

open System
open FsUnit
open NUnit.Framework
open DynamoDb.SQL
open DynamoDb.SQL.Parser
open DynamoDb.SQL.Execution

let equal = FsUnit.equal

[<TestFixture>]
type ``Given a V1 DynamoQuery`` () =
    [<Test>]
    member this.``when there is only a hash key equality filter it should return a QueryOperationConfig`` () =
        let dynamoQuery = parseDynamoQueryV1 "SELECT * FROM Employees WHERE @HashKey = \"Yan\""

        match dynamoQuery with
        | GetQueryConfig config 
            when config.HashKey.AsString() = "Yan" &&
                 config.Limit = Int32.MaxValue &&
                 config.Filter.Condition = null &&
                 config.AttributesToGet = null
            -> true
        | _ -> false
        |> should equal true

    [<Test>]
    member this.``when there is a hash key and a range key equality filter it should return a QueryOperationConfig`` () =
        let dynamoQuery = parseDynamoQueryV1 "SELECT * FROM Employees WHERE @HashKey = \"Yan\" AND @RangeKey = 30"

        match dynamoQuery with
        | GetQueryConfig config 
            when config.HashKey.AsString() = "Yan" &&
                 config.Limit = Int32.MaxValue && 
                 config.Filter.Condition.ComparisonOperator = "EQ" &&
                 config.Filter.Condition.AttributeValueList.Count = 1 &&
                 config.Filter.Condition.AttributeValueList.[0].N = "30" &&
                 config.AttributesToGet = null
            -> true
        | _ -> false
        |> should equal true

    [<Test>]
    member this.``when there is a hash key and a range key greater than filter it should return a QueryOperationConfig`` () =
        let dynamoQuery = parseDynamoQueryV1 "SELECT * FROM Employees WHERE @HashKey = \"Yan\" AND @RangeKey > 30"

        match dynamoQuery with
        | GetQueryConfig config 
            when config.HashKey.AsString() = "Yan" &&
                 config.Limit = Int32.MaxValue && 
                 config.Filter.Condition.ComparisonOperator = "GT" &&
                 config.Filter.Condition.AttributeValueList.Count = 1 &&
                 config.Filter.Condition.AttributeValueList.[0].N = "30" &&
                 config.AttributesToGet = null
            -> true
        | _ -> false
        |> should equal true

    [<Test>]
    member this.``when there is a hash key and a range key in filter it should return a QueryOperationConfig`` () =
        let dynamoQuery = parseDynamoQueryV1 "SELECT * FROM Employees WHERE @HashKey = \"Yan\" AND @RangeKey between 5 and 25"

        match dynamoQuery with
        | GetQueryConfig config 
            when config.HashKey.AsString() = "Yan" &&
                 config.Limit = Int32.MaxValue && 
                 config.Filter.Condition.ComparisonOperator = "BETWEEN" &&
                 config.Filter.Condition.AttributeValueList.Count = 2 &&
                 config.Filter.Condition.AttributeValueList.[0].N = "5" &&
                 config.Filter.Condition.AttributeValueList.[1].N = "25" &&
                 config.AttributesToGet = null
            -> true
        | _ -> false
        |> should equal true

    [<Test>]
    member this.``when there is a page size it should return a QueryOperationConfig with the limit set to that number`` () =
        let dynamoQuery = parseDynamoQueryV1 "SELECT * FROM Employees WHERE @HashKey = \"Yan\" AND @RangeKey between 5 and 25 with (pagesize(100))"

        match dynamoQuery with
        | GetQueryConfig config 
            when config.HashKey.AsString() = "Yan" &&
                 config.Limit = 100 &&
                 config.Filter.Condition.ComparisonOperator = "BETWEEN" &&
                 config.Filter.Condition.AttributeValueList.Count = 2 &&
                 config.Filter.Condition.AttributeValueList.[0].N = "5" &&
                 config.Filter.Condition.AttributeValueList.[1].N = "25" &&
                 config.AttributesToGet = null
            -> true
        | _ -> false
        |> should equal true

[<TestFixture>]
type ``Given a V1 DynamoScan`` () =
    [<Test>]
    member this.``when there is no where clause it should return a ScanOperationConfig`` () =
        let dynamoQuery = parseDynamoScanV1 "SELECT * FROM Employees"

        match dynamoQuery with
        | GetScanConfig config 
            when config.Limit = Int32.MaxValue &&
                 config.AttributesToGet = null &&
                 config.Filter.ToConditions().Count = 0
            -> true
        | _ -> false
        |> should equal true

    [<Test>]
    member this.``when there is a page size option it should return a ScanOperationConfig with limit set to that value`` () =
        let dynamoQuery = parseDynamoScanV1 "SELECT * FROM Employees with (pagesize(100))"

        match dynamoQuery with
        | GetScanConfig config 
            when config.Limit = 100 &&
                 config.AttributesToGet = null &&
                 config.Filter.ToConditions().Count = 0
            -> true
        | _ -> false
        |> should equal true

    [<Test>]
    member this.``when there is one attribute in filter it should return a ScanRequest`` () =
        let dynamoQuery = parseDynamoScanV1 "SELECT * FROM Employees WHERE FirstName = \"Yan\""

        match dynamoQuery with
        | GetScanConfig config 
            when config.Limit = Int32.MaxValue &&
                 config.AttributesToGet = null &&
                 config.Filter.ToConditions().Count = 1 &&
                 config.Filter.ToConditions().["FirstName"].ComparisonOperator = "EQ" &&
                 config.Filter.ToConditions().["FirstName"].AttributeValueList.Count = 1 &&
                 config.Filter.ToConditions().["FirstName"].AttributeValueList.[0].S = "Yan"
            -> true
        | _ -> false
        |> should equal true

    [<Test>]
    member this.``when there are multiple attributes in filter it should return a ScanRequest`` () =
        let dynamoQuery = parseDynamoScanV1 "SELECT * FROM Employees WHERE FirstName = \"Yan\" AND LastName != \"Cui\" AND Age >= 30"

        match dynamoQuery with
        | GetScanReq req when req.TableName = "Employees" &&
                              req.Limit = 0 &&
                              req.AttributesToGet = null &&
                              req.ScanFilter.Count = 3 &&
                              req.ScanFilter.["FirstName"].ComparisonOperator = "EQ" &&
                              req.ScanFilter.["FirstName"].AttributeValueList.Count = 1 &&
                              req.ScanFilter.["FirstName"].AttributeValueList.[0].S = "Yan" &&
                              req.ScanFilter.["LastName"].ComparisonOperator = "NE" &&
                              req.ScanFilter.["LastName"].AttributeValueList.Count = 1 &&
                              req.ScanFilter.["LastName"].AttributeValueList.[0].S = "Cui" &&
                              req.ScanFilter.["Age"].ComparisonOperator = "GE" &&
                              req.ScanFilter.["Age"].AttributeValueList.Count = 1 &&
                              req.ScanFilter.["Age"].AttributeValueList.[0].N = "30"
            -> true
        | _ -> false
        |> should equal true