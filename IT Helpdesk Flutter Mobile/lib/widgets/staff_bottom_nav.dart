import 'package:flutter/material.dart';

import '../core/theme/app_theme.dart';
import '../screens/staff_dashboard_screen.dart';

class StaffBottomNav extends StatelessWidget {
  final int selectedIndex;
  final ValueChanged<int>? onDestinationSelected;

  const StaffBottomNav({
    super.key,
    required this.selectedIndex,
    this.onDestinationSelected,
  });

  void _navigate(BuildContext context, int index) {
    if (onDestinationSelected != null) {
      onDestinationSelected!(index);
      return;
    }

    Navigator.pushAndRemoveUntil(
      context,
      MaterialPageRoute(
        builder: (_) => StaffDashboardScreen(initialPage: index),
      ),
      (_) => false,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: AppTheme.navy,
        boxShadow: [
          BoxShadow(
            color: Color(0x22000000),
            blurRadius: 12,
            offset: Offset(0, -3),
          ),
        ],
      ),
      child: SafeArea(
        top: false,
        child: NavigationBarTheme(
          data: NavigationBarThemeData(
            backgroundColor: AppTheme.navy,
            indicatorColor: AppTheme.blue,
            height: 70,
            labelTextStyle: WidgetStateProperty.resolveWith<TextStyle>((
              states,
            ) {
              final selected = states.contains(WidgetState.selected);

              return TextStyle(
                fontSize: 12,
                fontWeight: selected ? FontWeight.bold : FontWeight.normal,
                color: selected ? Colors.white : const Color(0xFFCBD5E1),
              );
            }),
            iconTheme: WidgetStateProperty.resolveWith<IconThemeData>((states) {
              final selected = states.contains(WidgetState.selected);

              return IconThemeData(
                size: 24,
                color: selected ? Colors.white : const Color(0xFFCBD5E1),
              );
            }),
          ),
          child: NavigationBar(
            selectedIndex: selectedIndex.clamp(0, 2),
            backgroundColor: AppTheme.navy,
            indicatorColor: AppTheme.blue,
            elevation: 0,
            onDestinationSelected: (index) => _navigate(context, index),
            destinations: const [
              NavigationDestination(
                icon: Icon(Icons.home_outlined),
                selectedIcon: Icon(Icons.home),
                label: 'Home',
              ),
              NavigationDestination(
                icon: Icon(Icons.list_alt_outlined),
                selectedIcon: Icon(Icons.list_alt),
                label: 'My Tickets',
              ),
              NavigationDestination(
                icon: Icon(Icons.add_circle_outline),
                selectedIcon: Icon(Icons.add_circle),
                label: 'Report',
              ),
            ],
          ),
        ),
      ),
    );
  }
}
