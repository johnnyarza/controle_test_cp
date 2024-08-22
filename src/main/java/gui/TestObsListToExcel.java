package gui;

import java.io.FileOutputStream;
import java.io.IOException;

import javafx.beans.property.SimpleStringProperty;
import javafx.beans.property.StringProperty;
import javafx.collections.FXCollections;
import javafx.collections.*;

import org.apache.poi.ss.usermodel.*;
import org.apache.poi.xssf.usermodel.XSSFWorkbook;


public class TestObsListToExcel {

    // Example class representing the data model
    public static class Person {
        private final StringProperty name;
        private final StringProperty email;

        public Person(String name, String email) {
            this.name = new SimpleStringProperty(name);
            this.email = new SimpleStringProperty(email);
        }

        public String getName() {
            return name.get();
        }

        public void setName(String name) {
            this.name.set(name);
        }

        public String getEmail() {
            return email.get();
        }

        public void setEmail(String email) {
            this.email.set(email);
        }
    }

    public static void writeObservableListToExcel(ObservableList<Person> list, String filePath) throws IOException {
        try (Workbook workbook = new XSSFWorkbook()) {
            Sheet sheet = workbook.createSheet("Sheet1");

            // Create header row
            Row headerRow = sheet.createRow(0);
            Cell headerCell1 = headerRow.createCell(0);
            headerCell1.setCellValue("Name");
            Cell headerCell2 = headerRow.createCell(1);
            headerCell2.setCellValue("Email");

            // Populate data rows
            int rowNum = 1;
            for (Person person : list) {
                Row row = sheet.createRow(rowNum++);
                row.createCell(0).setCellValue(person.getName());
                row.createCell(1).setCellValue(person.getEmail());
            }

            // Write the output to a file
            try (FileOutputStream fileOut = new FileOutputStream(filePath)) {
                workbook.write(fileOut);
            }
        }
    }

    public static void main(String[] args) {
        // Create example data
        ObservableList<Person> people = FXCollections.observableArrayList(
            new Person("John Doe", "john@example.com"),
            new Person("Jane Smith", "jane@example.com")
        );

        // Convert ObservableList to Excel
        try {
            writeObservableListToExcel(people, "people.xlsx");
            System.out.println("Excel file created successfully.");
        } catch (IOException e) {
            e.printStackTrace();
        }
    }
}