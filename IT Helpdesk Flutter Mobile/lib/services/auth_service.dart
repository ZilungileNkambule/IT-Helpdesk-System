import 'package:supabase_flutter/supabase_flutter.dart';

class AuthService {
  final SupabaseClient _supabase = Supabase.instance.client;

  User? get currentUser => _supabase.auth.currentUser;

  Future<void> login({required String email, required String password}) async {
    final response = await _supabase.auth.signInWithPassword(
      email: email.trim(),
      password: password,
    );

    if (response.user == null) {
      throw Exception('Login failed. Please try again.');
    }

    final profile = await _supabase
        .from('profiles')
        .select('role, is_active')
        .eq('id', response.user!.id)
        .single();

    if (profile['role'] != 'staff' || profile['is_active'] != true) {
      await _supabase.auth.signOut();
      throw Exception('Only active staff members can access the mobile app.');
    }
  }

  Future<void> register({
    required String fullName,
    required String email,
    required String password,
  }) async {
    final response = await _supabase.auth.signUp(
      email: email.trim(),
      password: password,
      data: {'full_name': fullName.trim()},
    );

    if (response.user == null) {
      throw Exception('Registration failed.');
    }
  }

  Future<void> logout() async {
    await _supabase.auth.signOut();
  }
}
