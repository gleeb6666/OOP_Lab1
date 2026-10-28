using System.Numerics;
using System.Text;
using MyExcelMAUIApp.Core.Exceptions;
using MyExcelMAUIApp.Core.Model;
using MyExcelMAUIApp.Core.Storage;

namespace MyExcelMAUIApp.Tests;

public class SpreadsheetJsonSerializerTests
{
	private static CellAddress At(string name) => CellAddress.Parse(name);

	private static string Json(int rows, int columns, string cells, string format = SpreadsheetJsonSerializer.FormatName, int version = 1) =>
		$$"""{ "format": "{{format}}", "version": {{version}}, "rowCount": {{rows}}, "columnCount": {{columns}}, "cells": [{{cells}}] }""";

	[Fact]
	public void SerializeThenDeserialize_RestoresSizeExpressionsAndValues()
	{
		var original = new Spreadsheet(12, 7);
		original.SetExpression(At("A1"), "2^100");
		original.SetExpression(At("B2"), "A1 + mmin(3, C3)");
		original.SetExpression(At("G12"), "(1+");

		var restored = SpreadsheetJsonSerializer.Deserialize(SpreadsheetJsonSerializer.Serialize(original));

		Assert.Equal(12, restored.RowCount);
		Assert.Equal(7, restored.ColumnCount);
		Assert.Equal(3, restored.NonEmptyCells.Count());
		Assert.Equal("A1 + mmin(3, C3)", restored.GetCell(At("B2")).Expression);
		Assert.Equal(BigInteger.Pow(2, 100), restored.GetCell(At("B2")).Value);
		Assert.Equal(ErrorCodes.Syntax, restored.GetCell(At("G12")).DisplayValue);
	}

	[Fact]
	public async Task SerializeAsyncThenDeserializeAsync_ThroughStream()
	{
		var original = new Spreadsheet(3, 3);
		original.SetExpression(At("C3"), "inc(41)");

		using var stream = new MemoryStream();
		await SpreadsheetJsonSerializer.SerializeAsync(original, stream);
		stream.Position = 0;
		var restored = await SpreadsheetJsonSerializer.DeserializeAsync(stream);

		Assert.Equal(new BigInteger(42), restored.GetCell(At("C3")).Value);
	}

	[Fact]
	public void Serialize_WritesReadableJsonWithExpressions()
	{
		var sheet = new Spreadsheet(2, 2);
		sheet.SetExpression(At("B1"), "1 + 2");

		string json = SpreadsheetJsonSerializer.Serialize(sheet);

		Assert.Contains("\"format\": \"MyExcelMAUIApp.Spreadsheet\"", json);
		Assert.Contains("\"rowCount\": 2", json);
		Assert.Contains("\"address\": \"B1\"", json);
		Assert.Contains("\"expression\": \"1 + 2\"", json);
	}

	[Fact]
	public void Deserialize_EmptyCellsList_GivesEmptyTable()
	{
		var sheet = SpreadsheetJsonSerializer.Deserialize(Json(4, 5, ""));

		Assert.Equal(4, sheet.RowCount);
		Assert.Equal(5, sheet.ColumnCount);
		Assert.Empty(sheet.NonEmptyCells);
	}

	[Theory]
	[InlineData("not json at all")]
	[InlineData("{ \"rowCount\": ")]
	[InlineData("[1, 2, 3]")]
	[InlineData("null")]
	[InlineData("{ \"format\": \"Other\", \"version\": 1, \"rowCount\": 1, \"columnCount\": 1 }")]
	public void Deserialize_NotOurFile_Throws(string json)
	{
		Assert.Throws<SpreadsheetFormatException>(() => SpreadsheetJsonSerializer.Deserialize(json));
	}

	[Theory]
	[InlineData(0, 5)]
	[InlineData(5, 0)]
	[InlineData(Spreadsheet.MaxRowCount + 1, 5)]
	[InlineData(5, Spreadsheet.MaxColumnCount + 1)]
	public void Deserialize_InvalidSize_Throws(int rows, int columns)
	{
		Assert.Throws<SpreadsheetFormatException>(() => SpreadsheetJsonSerializer.Deserialize(Json(rows, columns, "")));
	}

	[Fact]
	public void Deserialize_NewerVersion_Throws()
	{
		var ex = Assert.Throws<SpreadsheetFormatException>(() => SpreadsheetJsonSerializer.Deserialize(Json(1, 1, "", version: 99)));
		Assert.Contains("версія", ex.Message);
	}

	[Theory]
	[InlineData("""{ "address": "1A", "expression": "1" }""")]
	[InlineData("""{ "address": "Z99", "expression": "1" }""")]
	[InlineData("""{ "expression": "1" }""")]
	[InlineData("""{ "address": "A1", "expression": "1" }, { "address": "a1", "expression": "2" }""")]
	public void Deserialize_InvalidCells_Throws(string cells)
	{
		Assert.Throws<SpreadsheetFormatException>(() => SpreadsheetJsonSerializer.Deserialize(Json(3, 3, cells)));
	}

	[Fact]
	public void Deserialize_RecalculatesReferences()
	{
		string cells = """{ "address": "B1", "expression": "A1 * 2" }, { "address": "A1", "expression": "21" }""";
		var sheet = SpreadsheetJsonSerializer.Deserialize(Json(2, 2, cells));

		Assert.Equal(new BigInteger(42), sheet.GetCell(At("B1")).Value);
	}

	[Fact]
	public async Task SavedFile_IsUtf8Text()
	{
		var sheet = new Spreadsheet(1, 1);
		sheet.SetExpression(At("A1"), "5");

		using var stream = new MemoryStream();
		await SpreadsheetJsonSerializer.SerializeAsync(sheet, stream);

		Assert.StartsWith("{", Encoding.UTF8.GetString(stream.ToArray()));
	}
}
