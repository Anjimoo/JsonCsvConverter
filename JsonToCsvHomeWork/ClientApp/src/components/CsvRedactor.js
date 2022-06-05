import React, { Component } from 'react';
import { TableRow } from './TableRow';
import './CsvRedactor.css';

export class CsvRedactor extends Component {
    static displayName = CsvRedactor.name;

    constructor(props) {
        super(props);
        this.state = {
            table: props.table
        }
        this.sendUpdatedTable = this.sendUpdatedTable.bind(this);
    }

    handleUpdate = async (event, key) => {
        this.setState(prevState => {
            return {
                table: prevState.table.map((row) => {
                    return row.key === key ? { ...row, newValue: event.target.value } : row
                })
            }
        }
        //    , async () => {
        //    await fetch('jsonconverter/update-json',
        //        {
        //            method: 'POST',
        //            headers: {
        //                'Accept': 'application/json',
        //                'Content-Type': 'application/json'
        //            },
        //            body: JSON.stringify(this.state.table)
        //        }
        //    )
        //}
        )
    }

    async sendUpdatedTable(event, key) {
        await fetch(`jsonconverter/update-json/${key}/${event.target.value}`,
            {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            }
        );
    }

    componentWillReceiveProps(nextProps) {
        this.setState({
            table: nextProps.table
        });
    }

    render() {
        let isJsonUploaded = this.state.table === null;
        console.log(this.state.table);
        return (
            <div className='csv-redactor'>
                {isJsonUploaded ?
                    <p>Upload Json to see CSV representation.</p>
                    :
                    <div className='edit-file-container'>
                        <h3>Edit json file</h3>
                        <div className='table-container'>
                            <table>
                                <thead>
                                    <tr>
                                        <th>Key</th>
                                        <th>Value</th>
                                        <th>New Value</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {this.state.table.map((row) => (<TableRow key={row.key} row={row}
                                        handleUpdate={this.handleUpdate}
                                        sendUpdatedTable={this.sendUpdatedTable}
                                    />))}
                                </tbody>
                            </table>
                        </div>
                        <button className='down-button'><b>Download Edited Json</b></button>
                    </div>
                }
            </div>
        );
    }
}
