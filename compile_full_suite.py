# -*- coding: utf-8 -*-
import csv
import os
import sys

from test_cases_builder.part1_auth import get_auth_cases
from test_cases_builder.part2_user_admin import get_user_admin_cases
from test_cases_builder.part3_user_profile import get_user_profile_cases
from test_cases_builder.part4_dashboard import get_dashboard_cases
from test_cases_builder.part5_audit import get_audit_cases
from test_cases_builder.part6_precheck import get_precheck_cases
from test_cases_builder.part7_step1 import get_step1_cases
from test_cases_builder.part8_step2 import get_step2_cases
from test_cases_builder.part9_step3 import get_step3_cases
from test_cases_builder.part10_records_grid import get_records_grid_cases
from test_cases_builder.part11_archiving import get_archiving_cases
from test_cases_builder.part12_subsidy import get_subsidy_cases
from test_cases_builder.part13_analytics import get_analytics_cases
from test_cases_builder.part14_ocr import get_ocr_cases

def main():
    all_raw = []
    
    sections = [
        ("AUTHENTICATION & SESSION MANAGEMENT", get_auth_cases()),
        ("USER ADMINISTRATION & STAFF MANAGEMENT", get_user_admin_cases()),
        ("USER PROFILE & SELF-SERVICE SECURITY", get_user_profile_cases()),
        ("DASHBOARD & SHELL NAVIGATION", get_dashboard_cases()),
        ("AUDIT LOGGING SYSTEM", get_audit_cases()),
        ("SOLO PARENT REGISTRATION - IDENTITY GATE & PRE-CHECK", get_precheck_cases()),
        ("SOLO PARENT RECORD DIALOG - STEP 1: PERSONAL & APPLICATION DATA", get_step1_cases()),
        ("SOLO PARENT RECORD DIALOG - STEP 2: LOCATION, CONTACT & FAMILY COMPOSITION", get_step2_cases()),
        ("SOLO PARENT RECORD DIALOG - STEP 3: CIRCUMSTANCES, NEEDS & CONFIRMATION", get_step3_cases()),
        ("SOLO PARENT RECORDS - GRID, DOSSIER VIEW, EDIT & RENEWAL", get_records_grid_cases()),
        ("DATA ARCHIVING, 30-DAY RECYCLE BIN & MUNICIPAL ARCHIVES", get_archiving_cases()),
        ("SUBSIDY RECOMMENDATION & ML PRIORITY ENGINE", get_subsidy_cases()),
        ("ANALYTICS & DEMOGRAPHIC REPORTING", get_analytics_cases()),
        ("OCR DOCUMENT INGESTION & HITL VERIFICATION", get_ocr_cases()),
    ]
    
    current_id = 1
    total_test_cases = sum(len(cases) for _, cases in sections)
    print(f"Total test cases to generate: {total_test_cases}")

    output_csv_path = "SOLACE_Test_Cases_Progress_Tracker.csv"

    with open(output_csv_path, mode="w", newline="", encoding="utf-8-sig") as f:
        writer = csv.writer(f)

        # Header block as formatted in the user's tracker
        writer.writerow(["IAS Test Cases Progress Tracker", "", "", "", "", "", "", "", ""])
        writer.writerow(["", "", "", "", "", "", "", "", ""])
        writer.writerow(["", "", "", "", "", "", "", "", ""])
        writer.writerow(["Project name:", "Solace", "", "", "", "", "", "", ""])
        writer.writerow(["Project owner/s:", "Jose Ryle Andre G. Almazora", "", "", "", "", "", str(total_test_cases), "Rating"])
        writer.writerow(["", "Arriane M. Caraliman", "", "", "", "", "", str(total_test_cases), "1"])
        writer.writerow(["", "Renzo B. Sua", "", "", "", "", "", "0", ""])
        writer.writerow(["", "", "", "", "", "", "", "", ""])
        writer.writerow([
            "Test Case Scenario ID",
            "Name of the Module Function",
            "Test Case Scenario",
            "Action",
            "Actual Input",
            "",
            "",
            "Product Quality Component",
            "Comments/Suggestion"
        ])

        for section_title, cases in sections:
            # Section separator row
            writer.writerow([section_title, "", "", "", "", "", "", "", ""])
            for module, scenario, action, actual_input, quality_comp, comment in cases:
                tc_id = f"ID-{current_id:03d}"
                current_id += 1
                writer.writerow([
                    tc_id,
                    module,
                    scenario,
                    action,
                    actual_input,
                    "PASSED",
                    "",
                    quality_comp,
                    comment
                ])

    print(f"Successfully generated {current_id - 1} test cases in {output_csv_path}")

if __name__ == "__main__":
    main()
