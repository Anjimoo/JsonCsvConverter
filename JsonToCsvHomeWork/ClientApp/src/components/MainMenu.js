import React, { Component } from 'react';
import { FileUploader } from './FileUploader';
import { CsvRedactor } from './CsvRedactor';
import './MainMenu.css';

export class MainMenu extends Component {
    static displayName = MainMenu.name;
    constructor() {
        super();
        this.handleTableUpdate = this.handleTableUpdate.bind(this);
    }
    state = {
        table: null
    }

    handleTableUpdate(data){
        this.setState({ table: data });
    }

    render() {
        return (
            <div className='main'>
                <FileUploader handleTableUpdate={this.handleTableUpdate} table={this.state.table} />
                <CsvRedactor table={this.state.table} />
            </div>
        );
    }
}