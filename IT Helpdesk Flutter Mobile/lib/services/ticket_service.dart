import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:supabase_flutter/supabase_flutter.dart';

class TicketService {
  // Replace with your running ASP.NET Core API address.
  // Android emulator example: https://10.0.2.2:YOUR_HTTPS_PORT
  static const String baseUrl = 'https://localhost:7226';

  Future<Map<String, String>> _headers() async {
    final supabase = Supabase.instance.client;

    var session = supabase.auth.currentSession;

    if (session == null) {
      throw Exception('Please log in again.');
    }

    if (session.isExpired) {
      final response = await supabase.auth.refreshSession();
      session = response.session;
    }

    if (session == null) {
      throw Exception('Your session has expired.');
    }

    return {
      'Authorization': 'Bearer ${session.accessToken}',
      'Content-Type': 'application/json',
    };
  }

  Future<List<Map<String, dynamic>>> getCategories() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/tickets/categories'),
      headers: await _headers(),
    );

    if (response.statusCode != 200) {
      throw Exception('Could not load categories: ${response.body}');
    }

    final data = jsonDecode(response.body) as List;

    return data.map((item) => Map<String, dynamic>.from(item)).toList();
  }

  Future<List<Map<String, dynamic>>> getMyTickets() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/tickets'),
      headers: await _headers(),
    );

    if (response.statusCode != 200) {
      throw Exception('Could not load tickets: ${response.body}');
    }

    final data = jsonDecode(response.body) as List;

    return data.map((item) => Map<String, dynamic>.from(item)).toList();
  }

  Future<void> createTicket({
    required int categoryId,
    required String categoryName,
    required String priority,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/tickets'),
      headers: await _headers(),
      body: jsonEncode({
        'title': '$categoryName Issue',
        'description': '$categoryName support requested',
        'categoryId': categoryId,
        'priority': priority,
      }),
    );

    if (response.statusCode != 201) {
      throw Exception('Ticket submission failed: ${response.body}');
    }
  }
}
